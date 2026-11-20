namespace BeQuestionBank.Application.Common.Interfaces.Services;

using System;
using System.IO;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Common;

public interface IImportService
{
    Task<ApiResponse<object>> PreviewWordAsync(Stream fileStream, string fileName);
    Task<ApiResponse<object>> PreviewZipAsync(Stream fileStream, string fileName);
    Task<ApiResponse<object>> ImportWordWithMediaAsync(Stream docxStream, string docxFileName, Stream? zipStream, string? zipFileName, Guid maMonHoc, Guid maNguoiTao);
    Task<ApiResponse<object>> ClearTempUploadsAsync();
}
