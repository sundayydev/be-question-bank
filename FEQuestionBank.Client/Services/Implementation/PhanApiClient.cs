using System.Net.Http.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Phan;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public class PhanApiClient : BaseApiClient, IPhanApiClient
    {
        public PhanApiClient(HttpClient httpClient) : base(httpClient) { }


        public Task<ApiResponse<PhanDto>> GetPhanByIdAsync(Guid id)
            => GetAsync<PhanDto>($"api/phan/{id}");

        public Task<ApiResponse<List<PhanDto>>> GetAllPhansAsync()
            => GetListAsync<PhanDto>("api/phan");

        public Task<ApiResponse<List<PhanDto>>> GetTreeAsync()
            => GetListAsync<PhanDto>("api/phan");

        public Task<ApiResponse<List<PhanDto>>> GetTreeByMonHocAsync(Guid maMonHoc)
            => GetListAsync<PhanDto>($"api/phan/monhoc/{maMonHoc}");

        public async Task<ApiResponse<CreatePhanDto>> CreatePhanAsync(CreatePhanDto model)
        {
            var res = await _httpClient.PostAsJsonAsync("api/phan", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<CreatePhanDto>>() 
                   ?? new ApiResponse<CreatePhanDto>(500, "Error");
        }

        public async Task<ApiResponse<UpdatePhanDto>> UpdatePhanAsync(Guid id, UpdatePhanDto model)
        {
            var res = await _httpClient.PatchAsJsonAsync($"api/phan/{id}", model);
            return await res.Content.ReadFromJsonAsync<ApiResponse<UpdatePhanDto>>() 
                   ?? new ApiResponse<UpdatePhanDto>(500, "Error");
        }

        public async Task<ApiResponse<string>> DeletePhanAsync(Guid id)
        {
            var res = await _httpClient.DeleteAsync($"api/phan/{id}");
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() 
                   ?? new ApiResponse<string>(500, "Error");
        }

        public async Task<ApiResponse<string>> SoftDeletePhanAsync(Guid id)
        {
            var res = await _httpClient.PatchAsync($"api/phan/{id}/XoaTam", null);
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() 
                   ?? new ApiResponse<string>(500, "Error");
        }

        public async Task<ApiResponse<string>> RestorePhanAsync(Guid id)
        {
            var res = await _httpClient.PatchAsync($"api/phan/{id}/KhoiPhuc", null);
            return await res.Content.ReadFromJsonAsync<ApiResponse<string>>() 
                   ?? new ApiResponse<string>(500, "Error");
        }
    }
}