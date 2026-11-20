namespace BeQuestionBank.Application.Common.Interfaces.Services;

public interface IToolService
{
    string ConvertToBase64(byte[] imageBytes, string contentType);
    string BuildImageHtml(string base64Src);
    string ConvertLatexToHtml(string content);
    string ProcessContentWithLatexAndMergeSpans(string content);
}
