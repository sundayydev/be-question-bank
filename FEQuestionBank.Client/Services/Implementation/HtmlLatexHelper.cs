using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace FEQuestionBank.Client.Services.Implementation;

public static class HtmlLatexHelper
{
    private static readonly Regex InlineMathRegex = new(@"\$([^$]+)\$", RegexOptions.Compiled);

    private static readonly Regex DisplayMathRegex =
        new(@"\$\$([^$]+)\$\$|\\\[([^\\\]]+)\\\]|\\begin\{([^}]+)\}(.*?)\\end\{\3\}",
            RegexOptions.Compiled | RegexOptions.Singleline);

    // Từ DB (HTML có span.math-inline/display) → nội dung sạch để người dùng edit
    public static string ToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Thay thế span.math-inline → $...$
        foreach (var node in doc.DocumentNode.SelectNodes(
                     "//span[@class='math-inline'] | //span[@class='math-display']") ?? Enumerable.Empty<HtmlNode>())
        {
            var isDisplay = node.GetClasses().Contains("math-display");
            var latex = node.InnerText.Trim();
            var replacement = isDisplay ? $$"""$${{latex}}$$""" : $$"""${{latex}}$""";
            node.ParentNode.ReplaceChild(doc.CreateTextNode(replacement), node);
        }

        return HttpUtility.HtmlDecode(doc.DocumentNode.InnerText);
    }

    // Từ nội dung người dùng edit → chuyển lại thành HTML chuẩn để lưu DB
    public static string ToRichHtml(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        var result = plainText;

        // Display math: $$...$$ hoặc \[...\]
        result = DisplayMathRegex.Replace(result, match =>
        {
            var latex = match.Groups.Cast<Group>().Skip(1).FirstOrDefault(g => g.Success)?.Value ?? "";
            return $"<span class=\"math-display\">[{latex}]</span>";
        });

        // Inline math: $...$
        result = InlineMathRegex.Replace(result, "<span class=\"math-inline\">[$1]</span>");

        // Bọc trong <p> nếu chưa có thẻ cha
        if (!result.TrimStart().StartsWith("<"))
            result = "<p>" + result + "</p>";

        return result.Replace("\n", "</p><p>").Replace("<p></p>", "");
    }
}