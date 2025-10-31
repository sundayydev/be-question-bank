using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Pagination;

using System.Net.Http.Json;
using System.Web;
using BEQuestionBank.Shared.DTOs.user;

namespace FEQuestionBank.Client.Services;

public class NguoiDungApiClient : BaseApiClient, INguoiDungApiClient
{
    public NguoiDungApiClient(HttpClient httpClient) : base(httpClient) { }

    public Task<ApiResponse<List<NguoiDungDto>>> GetAllNguoiDungsAsync()
        => GetListAsync<NguoiDungDto>("api/NguoiDung");
    
    public Task<ApiResponse<PagedResult<NguoiDungDto>>> GetNguoiDungsAsync(int page = 1, int limit = 10, string? sort = null, string? filter = null)
        => GetPagedAsync<NguoiDungDto>("api/khoa/paged", page, limit, sort, filter);
    
    public async Task<ApiResponse<NguoiDungDto>> CreateNguoiDungAsync(NguoiDungDto model)
    {
        var res = await _httpClient.PostAsJsonAsync("api/NguoiDung", model);
        return await res.Content.ReadFromJsonAsync<ApiResponse<NguoiDungDto>>() 
               ?? new ApiResponse<NguoiDungDto>(500, "Lỗi khi tạo người dùng");
    }

    public async Task<ApiResponse<NguoiDungDto>> UpdateNguoiDungAsync(string id, NguoiDungDto model)
    {
        var res = await _httpClient.PatchAsJsonAsync($"api/NguoiDung/{id}", model);
        return await res.Content.ReadFromJsonAsync<ApiResponse<NguoiDungDto>>()
               ?? new ApiResponse<NguoiDungDto>(500, "Lỗi khi cập nhật người dùng");
    }

    public async Task<ApiResponse<string>> DeleteNguoiDungAsync(string id)
    {
        var res = await _httpClient.DeleteAsync($"api/NguoiDung/{id}");
        return await res.Content.ReadFromJsonAsync<ApiResponse<string>>()
               ?? new ApiResponse<string>(500, "Lỗi khi xóa người dùng");
    }

    public async Task<ApiResponse<string>> LockNguoiDungAsync(string id)
    {
        var res = await _httpClient.PatchAsync($"api/NguoiDung/{id}/Khoa", null);
        return await res.Content.ReadFromJsonAsync<ApiResponse<string>>()
               ?? new ApiResponse<string>(500, "Lỗi khi khóa tài khoản");
    }

    public async Task<ApiResponse<string>> UnlockNguoiDungAsync(string id)
    {
        var res = await _httpClient.PatchAsync($"api/NguoiDung/{id}/MoKhoa", null);
        return await res.Content.ReadFromJsonAsync<ApiResponse<string>>()
               ?? new ApiResponse<string>(500, "Lỗi khi mở khóa tài khoản");
    }
}