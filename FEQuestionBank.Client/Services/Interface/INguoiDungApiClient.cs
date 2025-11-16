using BeQuestionBank.Shared.DTOs.Pagination;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.user;
using BEQuestionBank.Shared.DTOs.user;

namespace FEQuestionBank.Client.Services;

public interface INguoiDungApiClient
{
    Task<ApiResponse<List<NguoiDungDto>>> GetAllNguoiDungsAsync();
    Task<ApiResponse<PagedResult<NguoiDungDto>>> GetNguoiDungsPagedAsync(
        int page = 1, int pageSize = 10, string? sort = null, string? search = null);

    Task<ApiResponse<NguoiDungDto>> CreateNguoiDungAsync(NguoiDungDto model);
    Task<ApiResponse<NguoiDungDto>> UpdateNguoiDungAsync(Guid id, NguoiDungDto model);
    Task<ApiResponse<Guid>> DeleteNguoiDungAsync(Guid id);
    Task<ApiResponse<Guid>> LockNguoiDungAsync(Guid id);
    Task<ApiResponse<Guid>> UnlockNguoiDungAsync(Guid id);
    Task<ApiResponse<ImportResultDto>> ImportUsersAsync(MultipartFormDataContent content);

}