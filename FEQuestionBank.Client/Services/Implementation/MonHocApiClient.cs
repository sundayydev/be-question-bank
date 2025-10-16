using System.Net.Http.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public class MonHocApiClient : BaseApiClient, IMonHocApiClient
    {
        public MonHocApiClient(HttpClient httpClient) : base(httpClient) { }

        public Task<ApiResponse<List<MonHocDto>>> GetAllMonHocsAsync()
            => GetListAsync<MonHocDto>("api/monhoc");

        public Task<ApiResponse<PagedResult<MonHocDto>>> GetMonHocsAsync(int page = 1, int limit = 10, string? sort = null, string? filter = null)
            => GetPagedAsync<MonHocDto>("api/monhoc/paged", page, limit, sort, filter);

        public async Task<ApiResponse<MonHocDto>> GetMonHocByIdAsync(string id)
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<MonHocDto>>($"api/monhoc/{id}");
            return res ?? new ApiResponse<MonHocDto>(500, "Error");
        }

        public async Task<ApiResponse<MonHocDto>> CreateMonHocAsync(CreateMonHocDto model)
        {
            var res = await _httpClient.PostAsJsonAsync("api/monhoc", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<MonHocDto>>() ?? new(500, "Error");
        }
        public async Task<ApiResponse<List<MonHocDto>>> GetMonHocsByMaKhoaAsync(string maKhoa)
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<MonHocDto>>>($"api/monhoc/khoa/{maKhoa}");
            return res ?? new ApiResponse<List<MonHocDto>>(500, "Error");
        }

        public async Task<ApiResponse<MonHocDto>> UpdateMonHocAsync(string id, UpdateMonHocDto model)
        {
            var res = await _httpClient.PatchAsJsonAsync($"api/monhoc/{id}", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<MonHocDto>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<string>> DeleteMonHocAsync(string id)
        {
            var res = await _httpClient.DeleteAsync($"api/monhoc/{id}");
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<string>> SoftDeleteMonHocAsync(string id)
        {
            var res = await _httpClient.PatchAsync($"api/monhoc/{id}/XoaTam", null);
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<string>> RestoreMonHocAsync(string id)
        {
            var res = await _httpClient.PatchAsync($"api/monhoc/{id}/KhoiPhuc", null);
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
        }
    }
}