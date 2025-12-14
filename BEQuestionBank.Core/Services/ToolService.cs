using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;

namespace BEQuestionBank.Core.Services;

public class ToolService
{
    /// <summary>
    /// Chuyển file byte sang Base64
    /// </summary>
    public string ConvertToBase64(byte[] imageBytes, string contentType)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("File rỗng");

        return $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
    }

    /// <summary>
    /// Build HTML nhúng ảnh
    /// </summary>
    public string BuildImageHtml(string base64Src)
    {
        return
            $"<span class='image-wrapper'><img src=\"{base64Src}\" style=\"max-width:100%; height:auto; display:block; margin: 10px 0;\" /></span>";
    }

    // Regex LaTeX: $...$ for inline and $$...$$ for display math
    private readonly Regex _latexInlinePattern = new Regex(@"\$([^\$]+)\$", RegexOptions.Compiled);
    private readonly Regex _latexDisplayPattern = new Regex(@"\$\$([^\$]+)\$\$", RegexOptions.Compiled);

    // Regex LaTeX with \(...\) and \[...\]
    private readonly Regex _latexInlineParenPattern = new Regex(@"\\\((.+?)\\\)", RegexOptions.Compiled);

    private readonly Regex _latexDisplayBracketPattern =
        new Regex(@"\\\[(.+?)\\\]", RegexOptions.Compiled | RegexOptions.Singleline);


    /// <summary>
    /// Chuyển đổi các biểu thức LaTeX trong nội dung thành HTML hỗ trợ MathJax/KaTeX
    /// Hỗ trợ: $...$, \(...\), $$...$$, \[...\]
    /// </summary>
    public string ConvertLatexToHtml(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        string result = content;

        // Display math: $$...$$
        result = _latexDisplayPattern.Replace(result, match =>
        {
            string latex = match.Groups[1].Value.Trim();
            return $@"<span class='math-display'>\[{latex}\]</span>";
        });

        // Display math: \[...\]
        // Xử lý display math với \[...\] - chỉ convert nếu chưa được wrap trong span math-display
        result = _latexDisplayBracketPattern.Replace(result, match =>
        {
            // Kiểm tra xem có nằm trong <span class='math-display'> không bằng cách kiểm tra context
            int matchIndex = match.Index;
            string beforeMatch = result.Substring(Math.Max(0, matchIndex - 150), Math.Min(150, matchIndex));

            // Đếm số lượng <span class='math-display'> và </span> trước match
            int openMathSpans = Regex.Matches(beforeMatch,
                @"<span[^>]*class=['""]math-display['""][^>]*>", RegexOptions.IgnoreCase).Count;
            int closeSpans = Regex.Matches(beforeMatch,
                @"</span>", RegexOptions.IgnoreCase).Count;

            // Nếu có <span class='math-display'> chưa đóng, đã được convert rồi
            if (openMathSpans > closeSpans)
            {
                return match.Value; // Đã được convert rồi, giữ nguyên
            }

            string latex = match.Groups[1].Value.Trim();
            return $@"<span class='math-display'>\[{latex}\]</span>";
        });

        // Xử lý inline math với \(...\) trước
        // Chỉ convert nếu chưa được wrap trong span math-inline
        result = _latexInlineParenPattern.Replace(result, match =>
        {
            int matchIndex = match.Index;
            string beforeMatch = result.Substring(Math.Max(0, matchIndex - 150), Math.Min(150, matchIndex));

            int openMathSpans = Regex.Matches(beforeMatch, @"<span[^>]*class=['""]math-(?:inline|display)['""][^>]*>",
                RegexOptions.IgnoreCase).Count;
            int closeSpans = Regex.Matches(beforeMatch, @"</span>", RegexOptions.IgnoreCase).Count;

            if (openMathSpans > closeSpans)
            {
                return match.Value;
            }

            string latex = match.Groups[1].Value.Trim();
            return $@"<span class='math-inline'>\({latex}\)</span>";
        });

        // Inline math: $...$ (không phải $$)
        // Xử lý inline math ($...$) - chỉ convert nếu chưa được wrap
        result = _latexInlinePattern.Replace(result, match =>
        {
            int matchIndex = match.Index;
            string beforeMatch = result.Substring(Math.Max(0, matchIndex - 150), Math.Min(150, matchIndex));

            int openMathSpans = Regex.Matches(beforeMatch,
                @"<span[^>]*class=['""]math-(?:inline|display)['""][^>]*>", RegexOptions.IgnoreCase).Count;
            int closeSpans = Regex.Matches(beforeMatch,
                @"</span>", RegexOptions.IgnoreCase).Count;

            if (openMathSpans > closeSpans)
            {
                return match.Value; // Đã được convert rồi, giữ nguyên
            }

            string latex = match.Groups[1].Value.Trim();
            return $@"<span class='math-inline'>\({latex}\)</span>";
        });


        return result;
    }

    // === 2. Gộp các span liền kề có class chứa "text-content" ===
    private static readonly Regex _adjacentTextContentSpansPattern = new(
        @"(<span\b[^>]*class=['""][^'""]*text-content[^'""]*['""][^>]*>)
      (?:\s*
          (.*?)</span>\s*
          <span\b[^>]*class=['""][^'""]*text-content[^'""]*['""][^>]*>
      )*
      (.*?)</span>",
        RegexOptions.IgnoreCase
        | RegexOptions.Singleline
        | RegexOptions.Compiled
        | RegexOptions.IgnorePatternWhitespace
    );


    /// <summary>
    /// Gộp tất cả các thẻ <span class="... text-content ..."> liền kề nhau thành một span duy nhất
    /// Giúp tránh việc nội dung bị tách đoạn trong các editor rich text
    /// </summary>
    public string MergeAdjacentTextContentSpans(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        return _adjacentTextContentSpansPattern.Replace(html, match =>
        {
            string openingTag = match.Groups[1].Value; // Thẻ mở của span đầu tiên

            // Nội dung của span cuối cùng
            string combinedContent = match.Groups[3].Value;

            // Các nội dung của các span ở giữa (nếu có)
            CaptureCollection middleCaptures = match.Groups[2].Captures;
            for (int i = middleCaptures.Count - 1; i >= 0; i--) // Đảo ngược để giữ thứ tự đúng
            {
                combinedContent = middleCaptures[i].Value + combinedContent;
            }

            return $"{openingTag}{combinedContent}</span>";
        });
    }

    // === Hàm hỗ trợ ===
    private static string EscapeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    // === Hàm tiện ích kết hợp cả hai bước (nếu bạn hay dùng cùng lúc) ===
    /// <summary>
    /// Xử lý nội dung: chuyển LaTeX → HTML, sau đó gộp các span text-content liền kề
    /// </summary>
    public string ProcessContentWithLatexAndMergeSpans(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        string withLatex = ConvertLatexToHtml(content);
        return MergeAdjacentTextContentSpans(withLatex);
    }
}