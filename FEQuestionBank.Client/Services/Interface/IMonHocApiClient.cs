using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public interface IMonHocApiClient
    {
        Task<ApiResponse<List<MonHocDto>>> GetAllMonHocsAsync();
        Task<ApiResponse<PagedResult<MonHocDto>>> GetMonHocsPagedAsync
            (int page = 1, int pageSize = 10, string? sort = null, string? search = null);
        Task<ApiResponse<MonHocDto>> GetMonHocByIdAsync(Guid id);
        Task<ApiResponse<MonHocDto>> CreateMonHocAsync(CreateMonHocDto model);
        Task<ApiResponse<List<MonHocDto>>> GetMonHocsByMaKhoaAsync(Guid maKhoa);
        Task<ApiResponse<MonHocDto>> UpdateMonHocAsync(Guid id, UpdateMonHocDto model);
        Task<ApiResponse<string>> DeleteMonHocAsync(Guid id);
        Task<ApiResponse<string>> SoftDeleteMonHocAsync(Guid id);
        Task<ApiResponse<string>> RestoreMonHocAsync(Guid id);
        Task<ApiResponse<PagedResult<MonHocDto>>> GetTrashedMonHocsAsync(
            int page = 1,
            int pageSize = 20);
    }
}