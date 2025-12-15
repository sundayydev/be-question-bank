using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.Helpers;
using BEQuestionBank.Core.Repositories;
using Microsoft.Extensions.Logging;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BEQuestionBank.Core.Services
{
    public class DeThiExportForStudentService
    {
        private readonly IDeThiRepository _deThiRepository;
        private readonly IKhoaRepository _khoaRepository;
        private readonly ILogger<DeThiExportForStudentService> _logger;
        private static readonly Random _rnd = new Random();
        private const string EmbeddedTemplatePath =
            "BeQuestionBank.Shared.Templates.Exam.Word.TemplateHutech_Chuan_2025.dotx";

        public DeThiExportForStudentService(IDeThiRepository deThiRepository, IKhoaRepository khoaRepository, ILogger<DeThiExportForStudentService> logger)
        {
            _deThiRepository = deThiRepository;
            _khoaRepository = khoaRepository;
            _logger = logger;
        }

       
        public async Task<byte[]> ExportWordAsync(YeuCauXuatDeThiDto request)
        {
            var deThi = await _deThiRepository.GetFullForExportAsync(request.MaDeThi)
                        ?? throw new KeyNotFoundException("Không tìm thấy đề thi");

            var templateBytes = await EmbeddedFileHelper.GetBytesAsync(EmbeddedTemplatePath);
            if (templateBytes == null || templateBytes.Length == 0)
                throw new FileNotFoundException($"Không tìm thấy template Word: {EmbeddedTemplatePath}");

            using var ms = new MemoryStream(templateBytes);
            using var document = new WordDocument(ms, FormatType.Dotx);

            string maDe = request.MaDeThi.ToString("N")[..8].ToUpper();
            await ReplacePlaceholders(document, request, deThi, maDe);
            InsertQuestions(document, deThi, request.HoanViDapAn);

            using var output = new MemoryStream();
            document.Save(output, FormatType.Docx);
            return output.ToArray();
        }

        private async Task ReplacePlaceholders(WordDocument doc, YeuCauXuatDeThiDto request, DeThi deThi, string maDe)
        {
            doc.Replace("{{Ky}}", !string.IsNullOrEmpty(request.HocKy)
                ? $"{request.HocKy}.."
                : "", false, false);
            doc.Replace("{{NamHoc}}", request.NamHoc ?? $"..{DateTime.Now.Year}-{DateTime.Now.Year + 1}", false, false);
            string tenKhoa = await GetTenKhoaAsync(request.MaKhoa);
            doc.Replace("{{KhoaLop}}", tenKhoa, false, false);
            doc.Replace("{{MonThi}}", deThi.MonHoc?.TenMonHoc ?? "", false, false);
            doc.Replace("{{MaMonHoc}}", deThi.MonHoc?.MaSoMonHoc ?? "", false, false);
            doc.Replace("{{SoTC}}", deThi.MonHoc?.SoTinChi?.ToString() ?? "3", false, false);
            doc.Replace("{{NgayThi}}", request.NgayThi?.ToString("dd/MM/yyyy") ?? DateTime.Today.ToString("dd/MM/yyyy"), false, false);
            doc.Replace("{{ThoiGianLamBai}}", request.ThoiLuong.HasValue ? $"{request.ThoiLuong} phút" : "90 phút", false, false);
            doc.Replace("{{MaDe}}", maDe, false, false);

           
            doc.Replace("{{HinhThucThi}}",
                !string.IsNullOrWhiteSpace(request.HinhThucThi)
                    ? request.HinhThucThi
                    : "Trắc nghiệm", false, false);
        }
        private async Task<string> GetTenKhoaAsync(Guid? maKhoa)
        {
            if (!maKhoa.HasValue || maKhoa == Guid.Empty)
                return "Chưa xác định";

            var khoa = await _khoaRepository.GetByIdAsync(maKhoa.Value);
            return string.IsNullOrWhiteSpace(khoa?.TenKhoa)
                ? "Chưa xác định"
                : khoa.TenKhoa.Trim();
        }

        private void InsertQuestions(WordDocument doc, DeThi deThi, bool hoanViDapAn)
        {
            var section = doc.LastSection;
            var lines = BuildQuestionLines(deThi, hoanViDapAn);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    section.AddParagraph();
                    continue;
                }

                IWParagraph para = section.AddParagraph();
                para.ParagraphFormat.BeforeSpacing = 6;
                para.ParagraphFormat.AfterSpacing = 6;

                string html = line;

                // Xử lý audio trước: Thay thế thẻ <audio> bằng text đánh dấu
                html = Regex.Replace(html, 
                    @"<audio[^>]*>.*?</audio>", 
                    " [CÓ AUDIO] ", 
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                // Regex cập nhật: match cả <span class='image-wrapper'> và <img> trực tiếp
                var regex = new Regex(
                    @"(<span\s+class\s*=\s*['""]image-wrapper['""][^>]*>.*?<img[^>]+src\s*=\s*""data:image/[^;]+;base64,([^""]+)""[^>]*style\s*=\s*""([^""]*)""[^>]*>.*?</span>)|" +
                    @"(<img[^>]+src\s*=\s*""data:image/[^;]+;base64,([^""]+)""[^>]*style\s*=\s*""([^""]*)""[^>]*>)|" +
                    @"(<span class='math-display'>\\\[(.*?)\\\]</span>)",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                int pos = 0;

                while (pos < html.Length)
                {
                    Match m = regex.Match(html, pos);

                    // Text trước ảnh/math
                    string textBefore = m.Success
                        ? html.Substring(pos, m.Index - pos)
                        : html.Substring(pos);

                    // Kiểm tra xem text trước có kết thúc bằng <br/> không (trước khi xử lý)
                    bool hasBrBefore = !string.IsNullOrWhiteSpace(textBefore) &&
                                      Regex.IsMatch(textBefore, @"<br\s*/?>[\s]*$", RegexOptions.IgnoreCase);

                    if (!string.IsNullOrWhiteSpace(textBefore))
                    {
                        // Xóa <br/> ở cuối để không duplicate
                        string textToClean = hasBrBefore
                            ? Regex.Replace(textBefore, @"<br\s*/?>[\s]*$", "", RegexOptions.IgnoreCase)
                            : textBefore;

                        string cleanText = CleanHtmlText(textToClean);
                        if (!string.IsNullOrWhiteSpace(cleanText))
                            para.AppendText(cleanText);
                    }

                    if (!m.Success) break;

                    // Nếu có <br/> trước ảnh, tạo paragraph mới cho ảnh để xuống dòng
                    if (hasBrBefore && (m.Groups[1].Success || m.Groups[4].Success))
                    {
                        para = section.AddParagraph();
                        para.ParagraphFormat.BeforeSpacing = 6;
                        para.ParagraphFormat.AfterSpacing = 6;
                    }


                    if (m.Groups[1].Success || m.Groups[4].Success)
                    {
                        string base64 = m.Groups[1].Success ? m.Groups[2].Value : m.Groups[5].Value;
                        string style = m.Groups[1].Success ? m.Groups[3].Value : m.Groups[6].Value;
                        byte[] imgBytes = Convert.FromBase64String(base64);

                        IWPicture pic = para.AppendPicture(imgBytes);

                        // Luôn dùng Inline wrapping style
                        pic.TextWrappingStyle = TextWrappingStyle.Inline;

                        // Nếu có <br/> trước hoặc style có "display:block"/"margin:" thì tạo paragraph mới sau ảnh
                        // để ảnh xuống dòng riêng (đã tạo paragraph mới trước ảnh ở trên nếu có <br/>)
                        bool shouldBeBlock = hasBrBefore ||
                                            style.Contains("display:block", StringComparison.OrdinalIgnoreCase) ||
                                            style.Contains("margin:", StringComparison.OrdinalIgnoreCase);

                        if (shouldBeBlock)
                        {
                            // Tạo paragraph mới sau ảnh để text tiếp theo không dính vào ảnh
                            para = section.AddParagraph();
                            para.ParagraphFormat.BeforeSpacing = 6;
                            para.ParagraphFormat.AfterSpacing = 6;
                        }

                        // Lấy kích thước từ style (width/height)
                        var w = Regex.Match(style, @"width\s*:\s*(\d+)px", RegexOptions.IgnoreCase);
                        var h = Regex.Match(style, @"height\s*:\s*(\d+)px", RegexOptions.IgnoreCase);

                        if (w.Success) pic.Width = float.Parse(w.Groups[1].Value);
                        if (h.Success) pic.Height = float.Parse(h.Groups[1].Value);

                        // Giới hạn kích thước hợp lý
                        if (pic.Width == 0 || pic.Width > 500) pic.Width = 400;
                        if (pic.Height == 0 || pic.Height > 500) pic.Height = 350;
                    }
                    // === CÔNG THỨC TOÁN: vẫn để inline (giữ nguyên như cũ) ===
                    else if (m.Groups[7].Success)
                    {
                        string latex = m.Groups[8].Value;
                        WMath wMath = new WMath(doc);
                        wMath.MathParagraph.LaTeX = latex;
                        para.ChildEntities.Add(wMath);
                    }

                    pos = m.Index + m.Length;
                }
            }
        }

        private string CleanHtmlText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            // Xử lý audio: Thay thế thẻ <audio> bằng text đánh dấu
            html = Regex.Replace(html, 
                @"<audio[^>]*>.*?</audio>", 
                " [CÓ AUDIO] ", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Xóa span như cũ
            html = Regex.Replace(html, @"<span.*?>|</span>", "", RegexOptions.IgnoreCase);

            // QUAN TRỌNG: Chuyển <br/> thành xuống dòng thật (không xóa nữa!)
            html = Regex.Replace(html, @"<br\s*/?>", "\r\n", RegexOptions.IgnoreCase);

            // Decode các ký tự HTML như &nbsp;, &gt;, v.v.
            return System.Net.WebUtility.HtmlDecode(html).Trim();
        }
        

        
        public async Task<byte[]> ExportPdfAsync(YeuCauXuatDeThiDto request)
        {
            byte[] wordBytes = await ExportWordAsync(request);
            using var ms = new MemoryStream(wordBytes);
            using var doc = new WordDocument(ms, FormatType.Docx);
            using var renderer = new DocIORenderer();
            using PdfDocument pdf = renderer.ConvertToPDF(doc);
            using var output = new MemoryStream();
            pdf.Save(output);
            return output.ToArray();
        }
        

        #region Helpers
        private List<string> BuildQuestionLines(DeThi deThi, bool hoanVi)
        {
            var lines = new List<string>();
            int stt = 1;
            var groups = deThi.ChiTietDeThis?
                .Where(x => x.CauHoi != null && x.CauHoi.MaCauHoiCha == null) // Chỉ lấy câu hỏi cha
                .OrderBy(x => x.ThuTu)
                .GroupBy(x => x.Phan)
                .OrderBy(g => g.First().Phan?.ThuTu ?? 0);

            if (groups == null || !groups.Any())
            {
                lines.Add("Không có câu hỏi.");
                return lines;
            }

            foreach (var g in groups)
            {
                if (g.Key != null)
                {
                    lines.Add($"PHẦN {g.Key.ThuTu}: {g.Key.TenPhan?.ToUpper()}");
                    lines.Add("");
                }

                foreach (var ct in g.OrderBy(x => x.ThuTu))
                {
                    var ch = ct.CauHoi!;
                    
                    // Xử lý câu hỏi NH (Nhóm)
                    if (ch.LoaiCauHoi == "NH" && ch.CauHoiCons != null && ch.CauHoiCons.Any())
                    {
                        var children = ch.CauHoiCons
                            .Where(c => c.XoaTam != true)
                            .OrderBy(c => c.MaSoCauHoi)
                            .ThenBy(c => c.NgayTao)
                            .ToList();
                        
                        int childCount = children.Count;
                        int startNum = stt;
                        int endNum = stt + childCount - 1;
                        
                        // Đánh số trước câu hỏi cha: "1-3"
                        lines.Add($"{startNum}-{endNum}. {ch.NoiDung}");
                        lines.Add("");
                        
                        // Đánh số và hiển thị các câu hỏi con: 1, 2, 3
                        int childStt = 1;
                        foreach (var child in children)
                        {
                            lines.Add($"{stt:D2}. {child.NoiDung}");
                            
                            var answers = child.CauTraLois?
                                .GroupBy(a => a.MaCauTraLoi)
                                .Select(gr => gr.First())
                                .ToList() ?? new List<CauTraLoi>();
                            
                            if (hoanVi) Shuffle(answers);
                            
                            for (int i = 0; i < answers.Count; i++)
                            {
                                char label = (char)('A' + i);
                                lines.Add($" {label}. {answers[i].NoiDung}");
                            }
                            lines.Add("");
                            stt++;
                            childStt++;
                        }
                    }
                    // Xử lý câu hỏi TL (Tự luận) - hiển thị như bình thường
                    else if (ch.LoaiCauHoi == "TL")
                    {
                        lines.Add($"{stt:D2}. {ch.NoiDung}");
                        
                        // Câu hỏi TL có thể có câu hỏi con (câu hỏi phụ)
                        if (ch.CauHoiCons != null && ch.CauHoiCons.Any())
                        {
                            var children = ch.CauHoiCons
                                .Where(c => c.XoaTam != true)
                                .OrderBy(c => c.MaSoCauHoi)
                                .ThenBy(c => c.NgayTao)
                                .ToList();
                            
                            foreach (var child in children)
                            {
                                lines.Add($"   {child.NoiDung}");
                            }
                        }
                        
                        lines.Add("");
                        stt++;
                    }
                    // Xử lý các loại câu hỏi khác (TN, MN, DT, GN)
                    else
                    {
                        lines.Add($"{stt:D2}. {ch.NoiDung}");

                        var answers = ch.CauTraLois?
                            .GroupBy(a => a.MaCauTraLoi)
                            .Select(gr => gr.First())
                            .ToList() ?? new List<CauTraLoi>();

                        if (hoanVi) Shuffle(answers);

                        for (int i = 0; i < answers.Count; i++)
                        {
                            char label = (char)('A' + i);
                            lines.Add($" {label}. {answers[i].NoiDung}");
                        }
                        lines.Add("");
                        stt++;
                    }
                }
            }
            return lines;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rnd.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public string ConvertLinesToJsonSafeString(List<string> lines)
        {
            var obj = new { format = string.Join("\n", lines) };
            return JsonSerializer.Serialize(obj);
        }
        #endregion
    


        #region EZP Export
        public async Task<byte[]> ExportEzpAsync(YeuCauXuatDeThiDto request, string password)
        {
            // 1. Xuất Word trước
            byte[] wordBytes = await ExportWordAsync(request);

            // 2. Mã hóa AES toàn bộ file Word
            return AES_Encrypt(wordBytes, password);
        }

        // AES Encrypt helper
        private byte[] AES_Encrypt(byte[] data, string password)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            aes.IV = new byte[16]; // IV = 0, hoặc random nếu muốn
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.Close();
            return ms.ToArray();
        }

        public byte[] AES_Decrypt(byte[] encryptedData, string password)
        {
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            aes.IV = new byte[16];
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(encryptedData, 0, encryptedData.Length);
            cs.Close();
            return ms.ToArray();
        }
        #endregion


    }
}