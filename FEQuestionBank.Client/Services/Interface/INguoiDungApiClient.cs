using BeQuestionBank.Shared.DTOs.Pagination;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.user;
using BEQuestionBank.Shared.DTOs.user;

namespace FEQuestionBank.Client.Services;

public interface INguoiDungApiClient
{
    Task<ApiResponse<List<NguoiDungDto>>> GetAllNguoiDungsAsync();
    Task<ApiResponse<PagedResult<NguoiDungDto>>> GetNguoiDungsAsync(
        int page = 1, int limit = 10, string? sort = null, string? filter = null);

    Task<ApiResponse<NguoiDungDto>> CreateNguoiDungAsync(NguoiDungDto model);
    Task<ApiResponse<NguoiDungDto>> UpdateNguoiDungAsync(string id, NguoiDungDto model);
    Task<ApiResponse<string>> DeleteNguoiDungAsync(string id);
    Task<ApiResponse<string>> LockNguoiDungAsync(string id);
    Task<ApiResponse<string>> UnlockNguoiDungAsync(string id);
    Task<ApiResponse<ImportResultDto>> ImportUsersAsync(MultipartFormDataContent content);

}