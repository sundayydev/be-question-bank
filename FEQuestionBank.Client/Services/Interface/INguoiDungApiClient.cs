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

    Task<ApiResponse<NguoiDungDto>> CreateNguoiDungAsync(CreateNguoiDungDto model);
    Task<ApiResponse<NguoiDungDto>> UpdateNguoiDungAsync(Guid id, UpdateNguoiDungDto model);
    Task<ApiResponse<string>> DeleteNguoiDungAsync(Guid id);
    Task<ApiResponse<string>> LockNguoiDungAsync(Guid id);
    Task<ApiResponse<string>> UnlockNguoiDungAsync(Guid id);
    Task<ApiResponse<ImportResultDto>> ImportUsersAsync(MultipartFormDataContent content);

}