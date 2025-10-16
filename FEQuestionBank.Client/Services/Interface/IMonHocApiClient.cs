using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public interface IMonHocApiClient
    {
        Task<ApiResponse<List<MonHocDto>>> GetAllMonHocsAsync();
        Task<ApiResponse<PagedResult<MonHocDto>>> GetMonHocsAsync(int page = 1, int limit = 10, string? sort = null, string? filter = null);
        Task<ApiResponse<MonHocDto>> GetMonHocByIdAsync(string id);
        Task<ApiResponse<MonHocDto>> CreateMonHocAsync(CreateMonHocDto model);
        Task<ApiResponse<List<MonHocDto>>> GetMonHocsByMaKhoaAsync(string maKhoa);
        Task<ApiResponse<MonHocDto>> UpdateMonHocAsync(string id, UpdateMonHocDto model);
        Task<ApiResponse<string>> DeleteMonHocAsync(string id);
        Task<ApiResponse<string>> SoftDeleteMonHocAsync(string id);
        Task<ApiResponse<string>> RestoreMonHocAsync(string id);
    }
}