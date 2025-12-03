using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.Enums;
using System.Threading.Tasks;

namespace FEQuestionBank.Client.Services
{
    public interface IFileApiClient
    {
        Task<ApiResponse<PagedResult<FileDto>>> GetFilesPagedAsync(int page, int pageSize, string? sort, string? search, FileType? fileType);
        Task<ApiResponse<bool>> DeleteFileAsync(Guid id);
        Task<ApiResponse<bool>> RestoreFileAsync(Guid id); // Thêm nếu có tính năng khôi phục
    }
}