using System.Net.Http.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.DeThi;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services;

public class DeThiApiClient : BaseApiClient, IDeThiApiClient
{
    public DeThiApiClient(HttpClient httpClient) : base(httpClient) { }

    public Task<ApiResponse<List<DeThiDto>>> GetAllAsync()
        => GetListAsync<DeThiDto>("api/dethi");

    public Task<ApiResponse<PagedResult<DeThiDto>>> GetPagedAsync(int page = 1, int limit = 10, string? sort = null, string? filter = null)
        => GetPagedAsync<DeThiDto>("api/dethi/paged", page, limit, sort, filter);

    public Task<ApiResponse<DeThiDto>> GetByIdAsync(Guid id)
        => GetAsync<DeThiDto>($"api/dethi/{id}");

    public async Task<ApiResponse<DeThiDto>> CreateAsync(CreateDeThiDto model)
    {
        var res = await _httpClient.PostAsJsonAsync("api/dethi", model);
        return await res.Content.ReadFromJsonAsync<ApiResponse<DeThiDto>>() ?? new(500, "Error");
    }

    public async Task<ApiResponse<DeThiDto>> UpdateAsync(Guid id, UpdateDeThiDto model)
    {
        var res = await _httpClient.PatchAsJsonAsync($"api/dethi/{id}", model);
        return await res.Content.ReadFromJsonAsync<ApiResponse<DeThiDto>>() ?? new(500, "Error");
    }

    public async Task<ApiResponse<string>> DeleteAsync(Guid id)
    {
        var res = await _httpClient.DeleteAsync($"api/dethi/{id}");
        return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
    }

    public Task<ApiResponse<List<DeThiDto>>> GetByMonHocAsync(Guid maMonHoc)
        => GetListAsync<DeThiDto>($"api/dethi/MonHoc/{maMonHoc}");
    
    public async Task<ApiResponse<DeThiWithChiTietAndCauTraLoiDto>> GetByIdWithChiTietAndCauTraLoiAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"api/dethi/{id}/WithChiTietAndCauTraLoi");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
        }

        // Dùng GetFromJsonAsync → tự map camelCase
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<DeThiWithChiTietAndCauTraLoiDto>>();
        return apiResponse;
    }
}