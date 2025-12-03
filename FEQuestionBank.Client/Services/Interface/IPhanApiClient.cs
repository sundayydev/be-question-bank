using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.DTOs.Phan;

namespace FEQuestionBank.Client.Services
{
    public interface IPhanApiClient
    {
        Task<ApiResponse<PhanDto>> GetPhanByIdAsync(Guid id);
        Task<ApiResponse<List<PhanDto>>> GetAllPhansAsync();
        Task<ApiResponse<List<PhanDto>>> GetTreeAsync();
        Task<ApiResponse<List<PhanDto>>> GetTreeByMonHocAsync(Guid maMonHoc);
        Task<ApiResponse<CreatePhanDto>> CreatePhanAsync(CreatePhanDto model);
        Task<ApiResponse<UpdatePhanDto>> UpdatePhanAsync(Guid id, UpdatePhanDto model);
        Task<ApiResponse<List<PhanDto>>> GetPhanByMonHocAsync(Guid maMonHoc);
        Task<ApiResponse<string>> DeletePhanAsync(Guid id);
        Task<ApiResponse<string>> SoftDeletePhanAsync(Guid id);
        Task<ApiResponse<string>> RestorePhanAsync(Guid id);
        Task<ApiResponse<PagedResult<PhanDto>>> GetTrashedPhansAsync(
            int page = 1,
            int pageSize = 10);
    }
}