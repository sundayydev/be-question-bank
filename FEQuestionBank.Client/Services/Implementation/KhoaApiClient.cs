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

        public Task<ApiResponse<PagedResult<KhoaDto>>> GetKhoasPagedAsync
            (int page = 1, int pageSize = 10, string? sort = null, string? search = null)
            => GetPagedAsync<KhoaDto>("api/khoa/paged", page, pageSize, sort, search: search);

        public async Task<ApiResponse<KhoaDto>> CreateKhoaAsync(CreateKhoaDto model)
        {
            var res = await _httpClient.PostAsJsonAsync("api/khoa", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<KhoaDto>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<KhoaDto>> UpdateKhoaAsync(Guid id, UpdateKhoaDto model)
        {
            var res = await _httpClient.PatchAsJsonAsync($"api/khoa/{id}", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<KhoaDto>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<string>> DeleteKhoaAsync(Guid id)
        {
            var res = await _httpClient.DeleteAsync($"api/khoa/{id}");
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
        }
        

        public async Task<ApiResponse<string>> SoftDeleteKhoaAsync(Guid id)
        {
            var res = await _httpClient.PatchAsync($"api/khoa/{id}/XoaTam", null);
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() 
                   ?? new ApiResponse<string>(500, "Error");
        }

        public async Task<ApiResponse<string>> RestoreKhoaAsync(Guid id)
        {
            var res = await _httpClient.PatchAsync($"api/khoa/{id}/KhoiPhuc", null);
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() 
                   ?? new ApiResponse<string>(500, "Error");
        }
        public async Task<ApiResponse<PagedResult<KhoaDto>>> GetTrashedKhoasAsync(int page = 1, int pageSize = 20)
        {
            return await GetPagedAsync<KhoaDto>("api/khoa/trashed", page, pageSize);
        }
    }
}