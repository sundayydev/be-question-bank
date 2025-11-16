using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Pagination;

using System.Net.Http.Json;
using System.Web;
using BeQuestionBank.Shared.DTOs.user;
using BEQuestionBank.Shared.DTOs.user;

namespace FEQuestionBank.Client.Services;

public class NguoiDungApiClient : BaseApiClient, INguoiDungApiClient
{
    public NguoiDungApiClient(HttpClient httpClient) : base(httpClient) { }

    public Task<ApiResponse<List<NguoiDungDto>>> GetAllNguoiDungsAsync()
        => GetListAsync<NguoiDungDto>("api/nguoidung");
    
    public Task<ApiResponse<PagedResult<NguoiDungDto>>> GetNguoiDungsPagedAsync
        (int page = 1, int pageSize = 10, string? sort = null, string? search = null)
        => GetPagedAsync<NguoiDungDto>("api/NguoiDung/paged", page, pageSize, sort, search);
    
    public async Task<ApiResponse<NguoiDungDto>> CreateNguoiDungAsync(NguoiDungDto model)
    {
        var res = await _httpClient.PostAsJsonAsync("api/NguoiDung", model);
        return await res.Content.ReadFromJsonAsync<ApiResponse<NguoiDungDto>>() 
               ?? new ApiResponse<NguoiDungDto>(500, "Lỗi khi tạo người dùng");
    }

    public async Task<ApiResponse<NguoiDungDto>> UpdateNguoiDungAsync(Guid id, NguoiDungDto model)
    {
        var res = await _httpClient.PatchAsJsonAsync($"api/NguoiDung/{id}", model);
        return await res.Content.ReadFromJsonAsync<ApiResponse<NguoiDungDto>>()
               ?? new ApiResponse<NguoiDungDto>(500, "Lỗi khi cập nhật người dùng");
    }

    public async Task<ApiResponse<Guid>> DeleteNguoiDungAsync(Guid id)
    {
        var res = await _httpClient.DeleteAsync($"api/NguoiDung/{id}");
        return await res.Content.ReadFromJsonAsync<ApiResponse<Guid>>()
               ?? new ApiResponse<Guid>(500, "Lỗi khi xóa người dùng");
    }

    public async Task<ApiResponse<Guid>> LockNguoiDungAsync(Guid id)
    {
        var res = await _httpClient.PatchAsync($"api/NguoiDung/{id}/Khoa", null);
        return await res.Content.ReadFromJsonAsync<ApiResponse<Guid>>()
               ?? new ApiResponse<Guid>(500, "Lỗi khi khóa tài khoản");
    }

    public async Task<ApiResponse<Guid>> UnlockNguoiDungAsync(Guid id)
    {
        var res = await _httpClient.PatchAsync($"api/NguoiDung/{id}/MoKhoa", null);
        return await res.Content.ReadFromJsonAsync<ApiResponse<Guid>>()
               ?? new ApiResponse<Guid>(500, "Lỗi khi mở khóa tài khoản");
    }
    public async Task<ApiResponse<ImportResultDto>> ImportUsersAsync(MultipartFormDataContent content)
    {
        var response = await _httpClient.PostAsync("api/NguoiDung/Import", content);

        // Đảm bảo HTTP thành công (200-299)
        if (!response.IsSuccessStatusCode)
        {
            return new ApiResponse<ImportResultDto>(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync()
            );
        }

        // Đọc JSON → ApiResponse<ImportResultDto>
        return await response.Content.ReadFromJsonAsync<ApiResponse<ImportResultDto>>()
               ?? new ApiResponse<ImportResultDto>(500, "Lỗi deserialize response");
    }
}