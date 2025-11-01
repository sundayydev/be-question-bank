using Microsoft.AspNetCore.Http;

namespace BEQuestionBank.Shared.DTOs.user;

public class ImportUserFileDto
{
    public IFormFile File { get; set; }
}