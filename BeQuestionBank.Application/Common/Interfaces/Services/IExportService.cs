namespace BeQuestionBank.Application.Common.Interfaces.Services;

using System;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.DeThi;

public interface IExportService
{
    Task<(bool Success, string Message, DeThiExportDto? Data)> ExportDeThiToJsonAsync(Guid maDeThi);
    Task<(bool Success, string Message, string? JsonContent)> ExportDeThiToJsonStringAsync(Guid maDeThi, bool indented = true);
    Task<(bool Success, string Message, byte[]? FileContent, string FileName)> ExportDeThiToEzpFileAsync(Guid maDeThi);
    Task<(bool Success, string Message, byte[]? FileContent, string FileName)> ExportDeThiToEzpFileWithPasswordAsync(Guid maDeThi);
}
