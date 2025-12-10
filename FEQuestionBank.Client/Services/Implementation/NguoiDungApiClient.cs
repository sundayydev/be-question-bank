using System.Net;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Pagination;

using System.Net.Http.Json;
using System.Text.Json;
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
    
    public async Task<ApiResponse<NguoiDungDto>> CreateNguoiDungAsync(CreateNguoiDungDto model)
    {
        var res = await _httpClient.PostAsJsonAsync("api/NguoiDung", model);
        return await res.Content.ReadFromJsonAsync<ApiResponse<NguoiDungDto>>() 
               ?? new ApiResponse<NguoiDungDto>(500, "Lỗi khi tạo người dùng");
    }

    public async Task<ApiResponse<NguoiDungDto>> UpdateNguoiDungAsync(Guid id, UpdateNguoiDungDto model)
    {
        var res = await _httpClient.PatchAsJsonAsync($"api/NguoiDung/{id}", model);

        // Thành công → parse bình thường
        if (res.IsSuccessStatusCode)
        {
            return await res.Content.ReadFromJsonAsync<ApiResponse<NguoiDungDto>>()
                   ?? new ApiResponse<NguoiDungDto>(500, "Lỗi không xác định");
        }

        //  Bắt lỗi validation 400
        if (res.StatusCode == HttpStatusCode.BadRequest)
        {
            var json = await res.Content.ReadAsStringAsync();

            var errorObj = JsonSerializer.Deserialize<ValidationErrorResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var messages = errorObj?.Errors?
                .SelectMany(e => e.Value)
                .ToList();

            return new ApiResponse<NguoiDungDto>(400, string.Join("\n", messages));
        }

        // Các lỗi khác
        return new ApiResponse<NguoiDungDto>(
            (int)res.StatusCode,
            $"Lỗi server: {res.StatusCode}"
        );
    }


    public async Task<ApiResponse<string>> DeleteNguoiDungAsync(Guid id)
    {
        var res = await _httpClient.DeleteAsync($"api/NguoiDung/{id}");
        return await res.Content.ReadFromJsonAsync<ApiResponse<string>>()
               ?? new ApiResponse<string>(500, "Lỗi khi xóa người dùng");
    }

    public async Task<ApiResponse<string>> LockNguoiDungAsync(Guid id)
    {
        var res = await _httpClient.PatchAsync($"api/NguoiDung/{id}/Khoa", null);
        return await res.Content.ReadFromJsonAsync<ApiResponse<string>>()
               ?? new ApiResponse<string>(500, "Lỗi khi khóa tài khoản");
    }

    public async Task<ApiResponse<string>> UnlockNguoiDungAsync(Guid id)
    {
        var res = await _httpClient.PatchAsync($"api/NguoiDung/{id}/MoKhoa", null);
        return await res.Content.ReadFromJsonAsync<ApiResponse<string>>()
               ?? new ApiResponse<string>(500, "Lỗi khi mở khóa tài khoản");
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

    public class ValidationErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; }
    }

}