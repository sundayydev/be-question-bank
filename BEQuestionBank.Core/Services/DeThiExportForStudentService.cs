using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BEQuestionBank.Core.Services
{
    public class DeThiExportForStudentService
    {
        private readonly IDeThiRepository _deThiRepository;
        private readonly ILogger<DeThiExportForStudentService> _logger;
        private static readonly Random _rnd = new Random();
        private const string EmbeddedTemplatePath =
            "BeQuestionBank.Shared.Templates.Exam.Word.TemplateHutech_Chuan_2025.dotx";

        public DeThiExportForStudentService(IDeThiRepository deThiRepository, ILogger<DeThiExportForStudentService> logger)
        {
            _deThiRepository = deThiRepository;
            _logger = logger;
        }

        #region Word Export
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
            ReplacePlaceholders(document, request, deThi, maDe);
            InsertQuestions(document, deThi, request.HoanViDapAn);

            using var output = new MemoryStream();
            document.Save(output, FormatType.Docx);
            return output.ToArray();
        }

        private void ReplacePlaceholders(WordDocument doc, YeuCauXuatDeThiDto request, DeThi deThi, string maDe)
        {
            doc.Replace("{{TenTruong}}", "TRƯỜNG ĐẠI HỌC CÔNG NGHỆ TP.HCM (HUTECH)", false, false);
            doc.Replace("{{DeThiHocKy}}", !string.IsNullOrEmpty(request.HocKy)
                ? $"ĐỀ THI HỌC KỲ {request.HocKy}"
                : "ĐỀ THI KẾT THÚC HỌC PHẦN", false, false);
            doc.Replace("{{NamHoc}}", request.NamHoc ?? $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}", false, false);
            doc.Replace("{{KhoaLop}}", request.Lop ?? "", false, false);
            doc.Replace("{{MonThi}}", deThi.MonHoc?.TenMonHoc ?? "", false, false);
            doc.Replace("{{MaMonHoc}}", deThi.MonHoc?.MaSoMonHoc ?? "", false, false);
            doc.Replace("{{SoTC}}", deThi.MonHoc?.SoTinChi?.ToString() ?? "3", false, false);
            doc.Replace("{{NgayThi}}", request.NgayThi?.ToString("dd/MM/yyyy") ?? DateTime.Today.ToString("dd/MM/yyyy"), false, false);
            doc.Replace("{{ThoiGianLamBai}}", request.ThoiLuong.HasValue ? $"{request.ThoiLuong} phút" : "90 phút", false, false);
            doc.Replace("{{MaDe}}", maDe, false, false);
        }

        /// <summary>
        /// ẢNH NẰM ĐÚNG VỊ TRÍ TRONG NỘI DUNG – DÙNG INLINE WITH TEXT
        /// Giống hệt khi bạn chèn ảnh trong Word và để "In line with text"
        /// </summary>
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

                var regex = new Regex(
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

                    if (!string.IsNullOrWhiteSpace(textBefore))
                    {
                        string cleanText = CleanHtmlText(textBefore);
                        if (!string.IsNullOrWhiteSpace(cleanText))
                            para.AppendText(cleanText);
                    }

                    if (!m.Success) break;

                    // === ẢNH: INLINE WITH TEXT – NẰM ĐÚNG VỊ TRÍ TRONG DÒNG ===
                    if (m.Groups[1].Success)
                    {
                        string base64 = m.Groups[2].Value;
                        string style = m.Groups[3].Value;
                        byte[] imgBytes = Convert.FromBase64String(base64);

                        IWPicture pic = para.AppendPicture(imgBytes);

                        // ĐÚNG YÊU CẦU: ảnh chảy cùng chữ, không xuống dòng
                        pic.TextWrappingStyle = TextWrappingStyle.Inline;

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
                    else if (m.Groups[4].Success)
                    {
                        string latex = m.Groups[5].Value;
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

            // Xóa span như cũ
            html = Regex.Replace(html, @"<span.*?>|</span>", "", RegexOptions.IgnoreCase);

            // QUAN TRỌNG: Chuyển <br/> thành xuống dòng thật (không xóa nữa!)
            html = Regex.Replace(html, @"<br\s*/?>", "\r\n", RegexOptions.IgnoreCase);

            // Decode các ký tự HTML như &nbsp;, &gt;, v.v.
            return System.Net.WebUtility.HtmlDecode(html).Trim();
        }

        #endregion

        #region PDF Export
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
        #endregion

        #region Helpers
        private List<string> BuildQuestionLines(DeThi deThi, bool hoanVi)
        {
            var lines = new List<string>();
            int stt = 1;
            var groups = deThi.ChiTietDeThis?
                .Where(x => x.CauHoi != null)
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
                    lines.Add($"{stt:D2}. {ch.NoiDung}");

                    var answers = ch.CauTraLois?
                        .GroupBy(a => a.MaCauTraLoi)
                        .Select(g => g.First())
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
    }
}