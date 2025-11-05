// File: BeQuestionBank.Shared/DTOs/user/ImportResultDto.cs
namespace BeQuestionBank.Shared.DTOs.user;

public class ImportResultDto
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}