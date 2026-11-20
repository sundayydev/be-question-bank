namespace BeQuestionBank.Application.Common.Interfaces.Services;

using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

public interface IImportService
{
    Task<object> ImportQuestionsAsync(IFormFile wordFile, Guid maPhan, string? mediaFolderPath);
    Task<object> PreviewImportAsync(IFormFile wordFile, string? mediaFolderPath);
}
