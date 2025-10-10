using System.Net.Http.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public class KhoaApiClient : BaseApiClient, IKhoaApiClient
    {
        public KhoaApiClient(HttpClient httpClient) : base(httpClient) { }

        public Task<ApiResponse<List<KhoaDto>>> GetAllKhoasAsync()
            => GetListAsync<KhoaDto>("api/khoa");

        public Task<ApiResponse<PagedResult<KhoaDto>>> GetKhoasAsync(int page = 1, int limit = 10, string? sort = null, string? filter = null)
            => GetPagedAsync<KhoaDto>("api/khoa/paged", page, limit, sort, filter);

        public async Task<ApiResponse<KhoaDto>> CreateKhoaAsync(CreateKhoaDto model)
        {
            var res = await _httpClient.PostAsJsonAsync("api/khoa", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<KhoaDto>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<KhoaDto>> UpdateKhoaAsync(string id, UpdateKhoaDto model)
        {
            var res = await _httpClient.PatchAsJsonAsync($"api/khoa/{id}", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<KhoaDto>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<string>> DeleteKhoaAsync(string id)
        {
            var res = await _httpClient.DeleteAsync($"api/khoa/{id}");
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
        }
    }
}