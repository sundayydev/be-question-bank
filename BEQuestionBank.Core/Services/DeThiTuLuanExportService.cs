using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.Helpers;
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

                        AppendMixedContent(mainPara, cauHoiCha.NoiDung, true);

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

                                AppendMixedContent(subPara, cauCon.NoiDung, false);

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

        // ================= PLACEHOLDER (COPY STUDENT) =================
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

        // ================= HELPERS =================
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
        private void AppendMixedContent(IWParagraph paragraph, string? content, bool isBold)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            // Bước 1: Convert LaTeX to HTML (nếu cần)
            string cleaned = content;
            
            // Xóa các span wrapper HTML
            cleaned = Regex.Replace(cleaned, @"<span[^>]*class=['""]math-display['""][^>]*>(.*?)</span>", "$1", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"<span[^>]*class=['""]math-inline['""][^>]*>(.*?)</span>", "$1", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"<span[^>]*class=['""]text-content['""][^>]*>(.*?)</span>", "$1", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"<span[^>]*>(.*?)</span>", "$1", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, "<.*?>", string.Empty);

            // Bước 2: Detect pattern "...text: a) ... b) ..." 
            // Tìm vị trí của dấu hai chấm (":") trước câu a)
            var colonBeforeSubQ = Regex.Match(cleaned, @"^(.*?):\s*([a-d])\)\s*");
            
            if (colonBeforeSubQ.Success)
            {
                // Có pattern "đề bài: a) ..."
                // Append phần đề chính trước dấu ":":
                string mainQuestion = colonBeforeSubQ.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(mainQuestion))
                {
                    ProcessMixedContent(paragraph, mainQuestion + ":", isBold);
                }
                
                // Xuống dòng trước câu a)
                paragraph.AppendBreak(BreakType.LineBreak);
                
                // Xử lý phần còn lại (a), b), c)...)
                string remainingContent = cleaned.Substring(colonBeforeSubQ.Groups[1].Length + 1).TrimStart();
                ProcessSubQuestions(paragraph, remainingContent, isBold);
            }
            else
            {
                // Không có pattern "đề: a)", check xem có a), b) standalone không
                var subQuestionPattern = @"\s*([a-d])\)\s*";
                var parts = Regex.Split(cleaned, subQuestionPattern);

                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    // Có câu con a), b) nhưng không có dấu ":":
                    ProcessSubQuestions(paragraph, cleaned, isBold);
                }
                else
                {
                    // Không có câu con, xử lý bình thường
                    ProcessMixedContent(paragraph, cleaned, isBold);
                }
            }
        }

        private void ProcessSubQuestions(IWParagraph paragraph, string content, bool isBold)
        {
            var subQuestionPattern = @"\s*([a-d])\)\s*";
            var parts = Regex.Split(content, subQuestionPattern);

            if (parts.Length <= 1)
            {
                ProcessMixedContent(paragraph, content, isBold);
                return;
            }

            bool isFirstSubQ = true;
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 0)
                {
                    // Phần text
                    if (!string.IsNullOrWhiteSpace(parts[i]))
                    {
                        ProcessMixedContent(paragraph, parts[i], isBold);
                    }
                }
                else
                {
                    // Phần letter (a, b, c, d)
                    if (!isFirstSubQ)
                    {
                        // Thêm line break trước câu b), c), d)
                        paragraph.AppendBreak(BreakType.LineBreak);
                    }
                    isFirstSubQ = false;

                    // Append "a) " với indent - THÊM SPACES ĐỂ INDENT
                    var letter = paragraph.AppendText($"     {parts[i]}) ");
                    letter.CharacterFormat.Bold = isBold;
                }
            }
        }

        private void ProcessMixedContent(IWParagraph paragraph, string content, bool isBold)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            // Tìm LaTeX patterns
            var latexPattern = @"(\\\[[\s\S]*?\\\]|\\\([\s\S]*?\\\)|\$\$[\s\S]*?\$\$|(?<!\$)\$(?!\$)[^\$]+?\$(?!\$))";
            var matches = Regex.Matches(content, latexPattern, RegexOptions.Singleline);

            if (matches.Count == 0)
            {
                // Không có LaTeX, append text thuần
                var cleanedText = CleanTextContent(content);
                if (!string.IsNullOrWhiteSpace(cleanedText))
                {
                    IWTextRange textRange = paragraph.AppendText(cleanedText);
                    if (isBold)
                        textRange.CharacterFormat.Bold = true;
                    else
                        textRange.CharacterFormat.TextColor = Color.Black;
                }
                return;
            }

            // Có LaTeX - tách và append
            int currentIndex = 0;
            foreach (Match match in matches)
            {
                // Text trước LaTeX
                if (match.Index > currentIndex)
                {
                    string textBefore = content.Substring(currentIndex, match.Index - currentIndex);
                    var cleanedText = CleanTextContent(textBefore);
                    if (!string.IsNullOrWhiteSpace(cleanedText))
                    {
                        IWTextRange textRange = paragraph.AppendText(cleanedText);
                        if (isBold)
                            textRange.CharacterFormat.Bold = true;
                        else
                            textRange.CharacterFormat.TextColor = Color.Black;
                    }
                }

                // LaTeX - tạo WMath object
                string latex = match.Value.Trim();
                string pureLatex = StripLatexDelimiters(latex);
                pureLatex = NormalizeLatex(pureLatex);
                
                if (!string.IsNullOrWhiteSpace(pureLatex))
                {
                    try
                    {
                        WMath wMath = new WMath(paragraph.Document);
                        paragraph.ChildEntities.Add(wMath);
                        wMath.MathParagraph.LaTeX = pureLatex;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WMath ERROR] {ex.Message}");
                        IWTextRange fallbackRange = paragraph.AppendText($" [{pureLatex}] ");
                        fallbackRange.CharacterFormat.TextColor = Color.Red;
                    }
                }

                currentIndex = match.Index + match.Length;
            }

            // Text còn lại
            if (currentIndex < content.Length)
            {
                string textAfter = content.Substring(currentIndex);
                var cleanedText = CleanTextContent(textAfter);
                if (!string.IsNullOrWhiteSpace(cleanedText))
                {
                    IWTextRange textRange = paragraph.AppendText(cleanedText);
                    if (isBold)
                        textRange.CharacterFormat.Bold = true;
                    else
                        textRange.CharacterFormat.TextColor = Color.Black;
                }
            }
        }

        private static string StripLatexDelimiters(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return string.Empty;

            string result = latex.Trim();

            if (result.StartsWith(@"\[") && result.EndsWith(@"\]"))
                return result.Substring(2, result.Length - 4).Trim();
            
            if (result.StartsWith(@"\(") && result.EndsWith(@"\)"))
                return result.Substring(2, result.Length - 4).Trim();
            
            if (result.StartsWith("$$") && result.EndsWith("$$") && result.Length > 4)
                return result.Substring(2, result.Length - 4).Trim();
            
            if (result.StartsWith("$") && result.EndsWith("$") && result.Length > 2)
                return result.Substring(1, result.Length - 2).Trim();

            return result;
        }

        private static string NormalizeLatex(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return string.Empty;

            string result = latex;

            // Fix 1: Xóa khoảng trắng ngay sau backslash
            result = Regex.Replace(result, @"\\\s+", @"\");

            // Fix 2: Thay \mathbit -> \mathbf
            result = Regex.Replace(result, @"\\mathbit\s*\{([^}]*)\}", @"\mathbf{$1}", RegexOptions.IgnoreCase);

            // Fix 3: Xóa multiple spaces
            result = Regex.Replace(result, @"\s{2,}", " ");

            // Fix 4: Chuẩn hóa spacing
            result = result
                .Replace("{ ", "{")
                .Replace(" }", "}")
                .Replace("( ", "(")
                .Replace(" )", ")")
                .Replace(" ^", "^")
                .Replace(" _", "_");

            // Fix 5: Chuẩn hóa mũ/chỉ số
            result = Regex.Replace(result, @"\^\s*\{", "^{");
            result = Regex.Replace(result, @"_\s*\{", "_{");

            // Fix 6: Cân bằng ngoặc
            int open = result.Count(c => c == '{');
            int close = result.Count(c => c == '}');
            if (close > open)
            {
                while (close > open && result.EndsWith("}"))
                {
                    result = result.Substring(0, result.Length - 1);
                    close--;
                }
            }
            else if (open > close)
            {
                result += new string('}', open - close);
            }

            return result.Trim();
        }

        private static string CleanTextContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            return text
                .Replace("&nbsp;", " ")
                .Replace("&quot;", "\"")
                .Replace("&ldquo;", "\u201C")
                .Replace("&rdquo;", "\u201D")
                .Replace("&lsquo;", "\u2018")
                .Replace("&rsquo;", "\u2019")
                .Replace("&#39;", "'")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Trim();
        }
    }
}
