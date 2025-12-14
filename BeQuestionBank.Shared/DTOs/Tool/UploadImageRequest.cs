using Microsoft.AspNetCore.Http;

namespace BeQuestionBank.Shared.DTOs.Tool;

public class UploadImageRequest
{
    public IFormFile File { get; set; } = default!;
}