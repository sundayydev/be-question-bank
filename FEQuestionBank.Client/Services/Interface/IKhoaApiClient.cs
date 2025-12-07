using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public interface IKhoaApiClient
    {
        Task<ApiResponse<List<KhoaDto>>> GetAllKhoasAsync();
        Task<ApiResponse<KhoaDto>> GetByIdKhoaAsync(Guid id);
        Task<ApiResponse<PagedResult<KhoaDto>>> GetKhoasPagedAsync
            (int page = 1, int pageSize = 10, string? sort = null, string? filter = null);
        Task<ApiResponse<KhoaDto>> CreateKhoaAsync(CreateKhoaDto model);
        Task<ApiResponse<KhoaDto>> UpdateKhoaAsync(Guid id, UpdateKhoaDto model);
        Task<ApiResponse<string>> DeleteKhoaAsync(Guid id);
        Task<ApiResponse<string>> SoftDeleteKhoaAsync(Guid id);
        Task<ApiResponse<string>> RestoreKhoaAsync(Guid id);
        Task<ApiResponse<PagedResult<KhoaDto>>> GetTrashedKhoasAsync(
            int page = 1,
            int pageSize = 20);
        
    }
}