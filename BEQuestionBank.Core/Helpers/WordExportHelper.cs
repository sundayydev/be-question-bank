// File: BEQuestionBank.Core/Helpers/WordExportHelper.cs
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BEQuestionBank.Core.Helpers
{
    public static class WordExportHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Thêm nội dung hỗn hợp: text + ảnh base64 + LaTeX (dưới dạng ảnh PNG từ codecogs)
        /// </summary>
        public static async Task AppendMixedContentAsync(IWParagraph paragraph, string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return;

            // 1. Thay audio
            htmlContent = Regex.Replace(htmlContent,
                @"<audio[^>]*>.*?</audio>",
                " [CÓ AUDIO] ",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // 2. Regex tìm ảnh base64 và LaTeX
            var regex = new Regex(
                // Ảnh base64 trong wrapper hoặc trực tiếp
                @"(<span\s+class\s*=\s*['""]image-wrapper['""][^>]*>.*?<img[^>]+src\s*=\s*""data:image/[^;]+;base64,([^""]+)""[^>]*style\s*=\s*""([^""]*)""[^>]*>.*?</span>)|" +
                @"(<img[^>]+src\s*=\s*""data:image/[^;]+;base64,([^""]+)""[^>]*style\s*=\s*""([^""]*)""[^>]*>)|" +
                // LaTeX: \[ \] $$ $$ \( \) $ $
                @"(\\\[[\s\S]*?\\\]|\$\$[\s\S]*?\$\$|\\\([\s\S]*?\\\)|(?<!\$)\$(?!\$)[^\$]+?\$(?!\$))",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            int pos = 0;

            while (pos < htmlContent.Length)
            {
                Match m = regex.Match(htmlContent, pos);

                // Text thường trước
                string textBefore = m.Success
                    ? htmlContent.Substring(pos, m.Index - pos)
                    : htmlContent.Substring(pos);

                if (!string.IsNullOrWhiteSpace(textBefore))
                {
                    string cleanText = CleanHtmlText(textBefore);
                    if (!string.IsNullOrWhiteSpace(cleanText))
                        paragraph.AppendText(cleanText.Trim());
                }

                if (!m.Success) break;

                bool processed = false;

                // === XỬ LÝ ẢNH BASE64 ===
                if (m.Groups[1].Success || m.Groups[4].Success)
                {
                    string base64 = m.Groups[1].Success ? m.Groups[2].Value : m.Groups[5].Value;
                    string style = m.Groups[1].Success ? m.Groups[3].Value : m.Groups[6].Value;

                    try
                    {
                        byte[] imgBytes = Convert.FromBase64String(base64);
                        await InsertPicture(paragraph, imgBytes, style, isLatex: false);
                    }
                    catch
                    {
                        paragraph.AppendText("[LỖI ẢNH]");
                    }

                    processed = true;
                }
                
                // === XỬ LÝ LATEX → ẢNH ===
                else if (m.Groups[7].Success)
                {
                    string latex = m.Groups[7].Value;
                    string pureLatex = StripLatexDelimiters(latex).Trim();

                    if (!string.IsNullOrWhiteSpace(pureLatex))
                    {
                        // Xác định loại LaTeX (inline hay display)
                        bool isDisplayMode = latex.StartsWith("\\[") || latex.StartsWith("$$");
                        
                        // Encode LaTeX để đưa vào URL
                        string encoded = Uri.EscapeDataString(pureLatex);

                        // Tăng DPI để ảnh rõ nét hơn khi thu nhỏ
                        int dpi = isDisplayMode ? 300 : 250;
                        string url = $"https://latex.codecogs.com/png.image?\\dpi{{{dpi}}} {encoded}";

                        try
                        {
                            byte[] imageBytes = await _httpClient.GetByteArrayAsync(url);
                            // Chèn ảnh LaTeX với style phù hợp
                            await InsertPicture(paragraph, imageBytes, "", isLatex: true, isDisplayMode: isDisplayMode);
                        }
                        catch (Exception ex)
                        {
                            paragraph.AppendText($" [{pureLatex}] ");
                            Console.WriteLine($"LaTeX render error: {ex.Message}");
                        }
                    }

                    processed = true;
                }

                if (processed)
                    pos = m.Index + m.Length;
                else
                    pos++;
            }
        }

        private static async Task InsertPicture(IWParagraph paragraph, byte[] imageBytes, string style, bool isLatex = false, bool isDisplayMode = false)
        {
            IWPicture pic = paragraph.AppendPicture(imageBytes);
            pic.TextWrappingStyle = TextWrappingStyle.Inline;

            if (isLatex)
            {
                // Xử lý ảnh LaTeX - scale về kích thước như chữ
                // DPI cao (250-300) đảm bảo rõ nét khi thu nhỏ
                
                // Tính toán scale để ảnh có kích thước phù hợp với text
                float scaleFactor = isDisplayMode ? 0.30f : 0.25f;
                
                pic.Width = pic.Width * scaleFactor;
                pic.Height = pic.Height * scaleFactor;
                
                if (!isDisplayMode)
                {
                    // LaTeX inline mode: căn giữa với text
                    pic.VerticalOrigin = VerticalOrigin.Line;
                    pic.VerticalPosition = -2;
                }
            }
            else
            {
                // Xử lý ảnh base64 thông thường
                // Lấy width/height từ style
                var w = Regex.Match(style, @"width\s*:\s*(\d+)px", RegexOptions.IgnoreCase);
                var h = Regex.Match(style, @"height\s*:\s*(\d+)px", RegexOptions.IgnoreCase);

                if (w.Success) pic.Width = float.Parse(w.Groups[1].Value);
                if (h.Success) pic.Height = float.Parse(h.Groups[1].Value);

                // Giới hạn kích thước
                if (pic.Width == 0 || pic.Width > 600) pic.Width = 500;
                if (pic.Height == 0 || pic.Height > 500) pic.Height = 400;

                // Nếu có display:block hoặc margin → xuống dòng
                bool isBlock = style.Contains("display:block", StringComparison.OrdinalIgnoreCase) ||
                               style.Contains("margin:", StringComparison.OrdinalIgnoreCase);

                if (isBlock)
                {
                    paragraph.AppendBreak(BreakType.LineBreak);
                }
            }
        }

        private static string CleanHtmlText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            html = Regex.Replace(html, @"<br\s*/?>", "\r\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</?span[^>]*>", "", RegexOptions.IgnoreCase);
            return System.Net.WebUtility.HtmlDecode(html);
        }

        private static string StripLatexDelimiters(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return string.Empty;
            latex = latex.Trim();

            if ((latex.StartsWith("\\[") && latex.EndsWith("\\]")) ||
                (latex.StartsWith("$$") && latex.EndsWith("$$")))
                return latex.Substring(2, latex.Length - 4);

            if ((latex.StartsWith("\\(") && latex.EndsWith("\\)")) ||
                (latex.StartsWith("$") && latex.EndsWith("$")))
                return latex.Substring(1, latex.Length - 2);

            return latex;
        }
    }
}