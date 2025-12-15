using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Xceed.Words.NET;
using Xceed.Document.NET;
using System.IO;
using System.Text.RegularExpressions;
using BeQuestionBank.Shared.Enums;
using Xceed.Words.NET;

namespace BEQuestionBank.Core.Services;

public class DeThiTuLuanExportService
{
    private readonly IDeThiRepository _deThiRepository;
    private readonly ToolService _toolService;

    public DeThiTuLuanExportService(IDeThiRepository deThiRepository, ToolService toolService)
    {
        _deThiRepository = deThiRepository;
        _toolService = toolService; 
    }

    public async Task<(bool Success, string Message, MemoryStream? FileStream, string FileName)>
        ExportTuLuanToWordAsync(Guid maDeThi)
    {
        try
        {
            var deThi = await _deThiRepository.GetFullForExportAsync(maDeThi);
            if (deThi == null)
                return (false, "Không tìm thấy đề thi.", null, "");

            if (deThi.ChiTietDeThis == null || !deThi.ChiTietDeThis.Any())
                return (false, "Đề thi không có câu hỏi.", null, "");

            var chiTietSorted = deThi.ChiTietDeThis
                .OrderBy(x => x.ThuTu)
                .ToList();

            using var memoryStream = new MemoryStream();
            using (var doc = DocX.Create(memoryStream))
            {
                #region Page setup

                doc.PageLayout.Orientation = Orientation.Portrait;
                doc.MarginLeft = 70;
                doc.MarginRight = 50;
                doc.MarginTop = 50;
                doc.MarginBottom = 50;
                doc.SetDefaultFont(new Font("Times New Roman"), 13);

                #endregion


                var groupedParts = GroupQuestionsByPart(chiTietSorted);

                char partLetter = 'A';
                foreach (var part in groupedParts)
                {
                    var partTitle = doc.InsertParagraph($"{partLetter}. PHẦN {(partLetter - 'A' + 1)}");
                    partTitle.FontSize(14);
                    partTitle.Bold();
                    partTitle.SpacingBefore(20);
                    partTitle.SpacingAfter(12);

                    int mainIndex = 1;

                    foreach (var ct in part)
                    {
                        var cauHoiCha = ct.CauHoi;
                        if (cauHoiCha == null) continue;
                        string contentNormalized = _toolService.ConvertLatexToHtml(cauHoiCha.NoiDung);
                        var noiDungCha = StripHtml(contentNormalized);
                       // var noiDungCha = StripHtml(cauHoiCha.NoiDung);
                        if (string.IsNullOrWhiteSpace(noiDungCha)) continue;

                        var cloText = BuildCloText(cauHoiCha);

                        var content = cloText == null
                            ? $"{ToRoman(mainIndex++)}. {noiDungCha}"
                            : $"{ToRoman(mainIndex++)}. {noiDungCha} {cloText}";

                        var mainPara = doc.InsertParagraph(content);
                        mainPara.FontSize(13);
                        mainPara.Bold();
                        mainPara.SpacingAfter(8);

                        if (cauHoiCha.CauHoiCons != null && cauHoiCha.CauHoiCons.Any())
                        {
                            int subIndex = 1;
                            foreach (var cauCon in cauHoiCha.CauHoiCons)
                            {string subContentNormalized = _toolService.ConvertLatexToHtml(cauCon.NoiDung);
                                var noiDungCon = StripHtml(subContentNormalized);
                                //var noiDungCon = StripHtml(cauCon.NoiDung);
                                if (string.IsNullOrWhiteSpace(noiDungCon)) continue;

                                var subPara = doc.InsertParagraph($"     {subIndex++}. {noiDungCon}");
                                subPara.FontSize(13);
                                subPara.IndentationFirstLine = 1.0f;
                                subPara.SpacingAfter(6);
                            }
                        }
                    }

                    partLetter++;
                }

                doc.InsertParagraph().SpacingBefore(30);
                var endPara = doc.InsertParagraph("HẾT");
                endPara.FontSize(14);
                endPara.Bold();
                endPara.Alignment = Alignment.center;

                doc.Save();
            }

            var resultStream = new MemoryStream(memoryStream.ToArray());
            resultStream.Position = 0;

            var safeTenDe = GenerateSlug(deThi.TenDeThi ?? "DeThiTuLuan");
            var fileName = $"DeThi_TuLuan_{safeTenDe}_{maDeThi:N}.docx";

            return (true, "Xuất file Word thành công.", resultStream, fileName);
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi khi xuất file Word: {ex.Message}", null, "");
        }
    }

    #region Helpers

    private static string? BuildCloText(CauHoi cauHoi)
    {
        if (!cauHoi.CLO.HasValue)
            return null;

        return $"({cauHoi.CLO.Value})";
    }

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
                    current = new List<ChiTietDeThi>();
                }
            }

            current.Add(ct);
            prevOrder = ct.ThuTu;
        }

        if (current.Any()) groups.Add(current);
        return groups;
    }

    private static string? StripHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        return Regex.Replace(input, "<.*?>", string.Empty)
            .Replace("&nbsp;", " ")
            .Replace("&quot;", "\"")
            .Replace("&ldquo;", "“")
            .Replace("&rdquo;", "”")
            .Replace("&lsquo;", "‘")
            .Replace("&rsquo;", "’")
            .Replace("&#39;", "'")
            .Trim();
    }

    private static string GenerateSlug(string phrase)
        => Regex.Replace(phrase, "[^a-zA-Z0-9]+", "_").Trim('_');

    private static string ToRoman(int number)
    {
        if (number <= 0) return number.ToString();
        var map = new (int, string)[]
        {
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
            (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
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

    #endregion
}