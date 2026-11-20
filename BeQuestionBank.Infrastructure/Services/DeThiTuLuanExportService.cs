using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.Helpers;
using BEQuestionBank.Core.Helpers;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace BEQuestionBank.Core.Services
{
    public class DeThiTuLuanExportService
    {
        private readonly IDeThiRepository _deThiRepository;
        private readonly IKhoaRepository _khoaRepository;
        private readonly ToolService _toolService;

        private const string EmbeddedTemplatePath =
            "BeQuestionBank.Shared.Templates.Exam.Word.TemplateHutech_Chuan_2025.dotx";

        public DeThiTuLuanExportService(
            IDeThiRepository deThiRepository,
            IKhoaRepository khoaRepository,
            ToolService toolService)
        {
            _deThiRepository = deThiRepository;
            _khoaRepository = khoaRepository;
            _toolService = toolService;
        }

        public async Task<(bool Success, string Message, MemoryStream? FileStream, string FileName)>
            ExportTuLuanToWordAsync(YeuCauXuatDeThiDto request)
        {
            try
            {
                var deThi = await _deThiRepository.GetFullForExportAsync(request.MaDeThi);
                if (deThi == null)
                    return (false, "Không tìm thấy đề thi.", null, "");

                if (deThi.ChiTietDeThis == null || !deThi.ChiTietDeThis.Any())
                    return (false, "Đề thi không có câu hỏi.", null, "");

                // ===== LOAD TEMPLATE =====
                var templateBytes = await EmbeddedFileHelper.GetBytesAsync(EmbeddedTemplatePath);
                if (templateBytes == null || templateBytes.Length == 0)
                    return (false, "Không tìm thấy template Word.", null, "");

                using var templateStream = new MemoryStream(templateBytes);
                using var doc = new WordDocument(templateStream, FormatType.Dotx);

                // ===== REPLACE PLACEHOLDER (GIỐNG STUDENT) =====
                await ReplacePlaceholders(doc, request, deThi);

                var section = doc.LastSection;

                var chiTietSorted = deThi.ChiTietDeThis
                    .OrderBy(x => x.ThuTu)
                    .ToList();

                var groupedParts = GroupQuestionsByPart(chiTietSorted);
                char partLetter = 'A';

                foreach (var part in groupedParts)
                {
                    IWParagraph partTitle = section.AddParagraph();
                    partTitle.AppendText($"{partLetter}. PHẦN {(partLetter - 'A' + 1)}");
                    partTitle.ApplyStyle(BuiltinStyle.Heading1);
                    partTitle.ParagraphFormat.BeforeSpacing = 20f;
                    partTitle.ParagraphFormat.AfterSpacing = 12f;

                    int mainIndex = 1;

                    foreach (var ct in part)
                    {
                        var cauHoiCha = ct.CauHoi;
                        if (cauHoiCha == null) continue;

                        IWParagraph mainPara = section.AddParagraph();

                        IWTextRange num = mainPara.AppendText($"{ToRoman(mainIndex)}. ");
                        num.CharacterFormat.Bold = true;

                        await AppendMixedContentAsync(mainPara, cauHoiCha.NoiDung, true);

                        var cloText = BuildCloText(cauHoiCha);
                        if (cloText != null)
                        {
                            var clo = mainPara.AppendText($" {cloText}");
                            clo.CharacterFormat.Bold = true;
                        }

                        mainPara.ParagraphFormat.AfterSpacing = 8f;

                        if (cauHoiCha.CauHoiCons != null && cauHoiCha.CauHoiCons.Any())
                        {
                            int subIndex = 1;
                            foreach (var cauCon in cauHoiCha.CauHoiCons)
                            {
                                IWParagraph subPara = section.AddParagraph();

                                IWTextRange subNum =
                                    subPara.AppendText($"     {subIndex++}. ");
                                subNum.CharacterFormat.TextColor = Color.Black;

                                await AppendMixedContentAsync(subPara, cauCon.NoiDung, false);

                                subPara.ParagraphFormat.FirstLineIndent = 36f;
                                subPara.ParagraphFormat.AfterSpacing = 6f;
                            }
                        }

                        mainIndex++;
                    }

                    partLetter++;
                }

                IWParagraph endPara = section.AddParagraph();
                endPara.AppendText("HẾT");
                endPara.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
                endPara.ParagraphFormat.BeforeSpacing = 12f;

                var output = new MemoryStream();
                doc.Save(output, FormatType.Docx);
                output.Position = 0;

                var safeName = GenerateSlug(deThi.TenDeThi ?? "DeThiTuLuan");
                var fileName = $"DeThi_TuLuan_{safeName}_{request.MaDeThi:N}.docx";

                return (true, "Xuất file Word thành công.", output, fileName);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi khi xuất file Word: {ex.Message}", null, "");
            }
        }

        // PLACEHOLDER
        private async Task ReplacePlaceholders(
            WordDocument doc,
            YeuCauXuatDeThiDto request,
            DeThi deThi)
        {
            doc.Replace("{{Ky}}",
                !string.IsNullOrEmpty(request.HocKy)
                    ? $"{request.HocKy}.."
                    : "", false, false);

            doc.Replace("{{NamHoc}}",
                request.NamHoc ?? $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}", false, false);

            string tenKhoa = await GetTenKhoaAsync(request.MaKhoa);
            doc.Replace("{{KhoaLop}}", tenKhoa, false, false);

            doc.Replace("{{MonThi}}", deThi.MonHoc?.TenMonHoc ?? "", false, false);
            doc.Replace("{{MaMonHoc}}", deThi.MonHoc?.MaSoMonHoc ?? "", false, false);
            doc.Replace("{{SoTC}}", deThi.MonHoc?.SoTinChi?.ToString() ?? "3", false, false);
            doc.Replace("{{NgayThi}}",
                request.NgayThi?.ToString("dd/MM/yyyy")
                ?? DateTime.Today.ToString("dd/MM/yyyy"), false, false);

            doc.Replace("{{ThoiGianLamBai}}",
                request.ThoiLuong.HasValue
                    ? $"{request.ThoiLuong} phút"
                    : "90 phút", false, false);

            doc.Replace("{{MaDe}}",
                request.MaDeThi.ToString("N")[..8].ToUpper(), false, false);

            doc.Replace("{{HinhThucThi}}", "Tự luận", false, false);
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

        // HELPERS 
        private static string? BuildCloText(CauHoi cauHoi)
            => cauHoi.CLO.HasValue ? $"({cauHoi.CLO.Value})" : null;

        private static List<List<ChiTietDeThi>> GroupQuestionsByPart(List<ChiTietDeThi> chiTiets)
        {
            var groups = new List<List<ChiTietDeThi>>();
            var current = new List<ChiTietDeThi>();
            int? prevOrder = null;

            foreach (var ct in chiTiets)
            {
                if (prevOrder.HasValue && ct.ThuTu <= prevOrder.Value)
                {
                    if (current.Any())
                    {
                        groups.Add(current);
                        current = new();
                    }
                }
                current.Add(ct);
                prevOrder = ct.ThuTu;
            }

            if (current.Any()) groups.Add(current);
            return groups;
        }

        private static string GenerateSlug(string input)
            => Regex.Replace(input, "[^a-zA-Z0-9]+", "_").Trim('_');

        private static string ToRoman(int number)
        {
            if (number <= 0) return number.ToString();
            var map = new (int, string)[]
            {
                (1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),
                (100,"C"),(90,"XC"),(50,"L"),(40,"XL"),
                (10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I")
            };
            var result = "";
            foreach (var (v, s) in map)
                while (number >= v)
                {
                    result += s;
                    number -= v;
                }
            return result;
        }

        // ===== KHÔI PHỤC LOGIC LATEX VỚI WMath =====
        private async Task AppendMixedContentAsync(IWParagraph paragraph, string? content, bool isBold)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            // Dùng helper để chèn text + ảnh base64 + LaTeX (dạng ảnh PNG)
            await WordExportHelper.AppendMixedContentAsync(paragraph, content);

            // Nếu là câu hỏi cha → in đậm toàn bộ phần text (không ảnh hưởng đến ảnh)
            if (isBold)
            {
                foreach (var entity in paragraph.ChildEntities)
                {
                    if (entity is WTextRange textRange) // Syncfusion thường dùng WTextRange
                    {
                        textRange.CharacterFormat.Bold = true;
                    }
                }
            }
        }
    }
}
