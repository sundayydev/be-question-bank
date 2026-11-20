namespace BeQuestionBank.Application.Common.Interfaces.Services;

using BeQuestionBank.Domain.Enums;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.DTOs.Pagination;
using System;
using System.Threading.Tasks;

public interface IFileStorageService
{
    Task<PagedResult<FileDto>> GetFilesPagedAsync(int page, int pageSize, string? sort, string? search, FileType? fileType);
    Task<bool> DeleteFileAsync(Guid maFile);
    Task<Guid?> GetCauHoiIdByFileIdAsync(Guid maFile);
}
