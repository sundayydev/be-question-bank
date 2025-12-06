using System.Reflection;
using System.Text;

namespace BeQuestionBank.Shared.Helpers;

public static class EmbeddedFileHelper
{
    private static readonly Assembly Assembly = typeof(EmbeddedFileHelper).Assembly;

    public static async Task<byte[]> GetBytesAsync(string resourcePath)
    {
        // Convert "Templates/Exam/Word/Template.dotx" → ".Templates.Exam.Word.Template.dotx"
        var normalized = resourcePath
            .Replace("/", ".")
            .Replace("\\", ".");

        // Tìm resource phù hợp
        var resourceFullName = Assembly
            .GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith(normalized, StringComparison.OrdinalIgnoreCase));

        if (resourceFullName == null)
        {
            var list = string.Join("\n", Assembly.GetManifestResourceNames());
            throw new FileNotFoundException(
                $"Không tìm thấy resource: {normalized}\n" +
                $" Các resource đang tồn tại:\n{list}");
        }

        await using var stream = Assembly.GetManifestResourceStream(resourceFullName)
            ?? throw new FileNotFoundException($"Không mở được resource: {resourceFullName}");

        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }


    public static async Task<string> GetStringAsync(string resourcePath)
    {
        var bytes = await GetBytesAsync(resourcePath);

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            bytes = bytes[3..];

        return Encoding.UTF8.GetString(bytes);
    }
}
