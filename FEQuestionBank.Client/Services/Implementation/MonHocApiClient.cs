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

        public Task<ApiResponse<PagedResult<MonHocDto>>> GetMonHocsPagedAsync
            (int page = 1, int limit = 10, string? sort = null, string? search = null)
            => GetPagedAsync<MonHocDto>("api/monhoc/paged", page, limit, sort, search);

        public async Task<ApiResponse<MonHocDto>> GetMonHocByIdAsync(Guid id)
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<MonHocDto>>($"api/monhoc/{id}");
            return res ?? new ApiResponse<MonHocDto>(500, "Error");
        }

        public async Task<ApiResponse<MonHocDto>> CreateMonHocAsync(CreateMonHocDto model)
        {
            var res = await _httpClient.PostAsJsonAsync("api/monhoc", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<MonHocDto>>() ?? new(500, "Error");
        }
        public async Task<ApiResponse<List<MonHocDto>>> GetMonHocsByMaKhoaAsync(Guid maKhoa)
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<MonHocDto>>>($"api/monhoc/khoa/{maKhoa}");
                // Nếu response null thì trả về rỗng
                return res ?? new ApiResponse<List<MonHocDto>>(200, "Không có môn học", new List<MonHocDto>());
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Nếu server trả 404 => không có môn học, trả về rỗng
                return new ApiResponse<List<MonHocDto>>(200, "Không có môn học", new List<MonHocDto>());
            }
            catch (Exception)
            {
                return new ApiResponse<List<MonHocDto>>(500, "Error");
            }
        }


        public async Task<ApiResponse<MonHocDto>> UpdateMonHocAsync(Guid id, UpdateMonHocDto model)
        {
            var res = await _httpClient.PatchAsJsonAsync($"api/monhoc/{id}", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<MonHocDto>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<string>> DeleteMonHocAsync(Guid id)
        {
            var res = await _httpClient.DeleteAsync($"api/monhoc/{id}");
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<string>> SoftDeleteMonHocAsync(Guid id)
        {
            var res = await _httpClient.PatchAsync($"api/monhoc/{id}/XoaTam", null);
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
        }

        public async Task<ApiResponse<string>> RestoreMonHocAsync(Guid id)
        {
            var res = await _httpClient.PatchAsync($"api/monhoc/{id}/KhoiPhuc", null);
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() ?? new(500, "Error");
        }
        public async Task<ApiResponse<PagedResult<MonHocDto>>> GetTrashedMonHocsAsync(int page = 1, int pageSize = 20)
        {
            return await GetPagedAsync<MonHocDto>("api/monhoc/trashed", page, pageSize);
        }
    }
}