using System.Net.Http.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace FEQuestionBank.Client.Services
{
    public class BaseApiClient
    {
        protected readonly HttpClient _httpClient;

        public BaseApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected async Task<ApiResponse<PagedResult<T>>> GetPagedAsync<T>(
            string url, int page, int limit, string? sort, string? filter)
        {
            var fullUrl = $"{url}?page={page}&limit={limit}";
            if (!string.IsNullOrEmpty(sort)) fullUrl += $"&sort={sort}";
            if (!string.IsNullOrEmpty(filter)) fullUrl += $"&filter={filter}";

            var res = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<T>>>(fullUrl);
            return res ?? new ApiResponse<PagedResult<T>>(500, "Error");
        }

        protected async Task<ApiResponse<List<T>>> GetListAsync<T>(string url)
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<T>>>(url);
            return res ?? new ApiResponse<List<T>>(500, "Error");
        }
    }
}