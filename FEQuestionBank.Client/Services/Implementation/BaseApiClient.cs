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
            string url,
            int page,
            int pageSize,
            string? sort = null,
            string? filter = null,
            string? search = null,
            bool? daXuLy = null)  
        {
            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            query["page"] = page.ToString();
            query["pageSize"] = pageSize.ToString(); 
            if (!string.IsNullOrEmpty(sort)) query["sort"] = sort;
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            else if (!string.IsNullOrEmpty(filter)) query["search"] = filter;
            if (daXuLy.HasValue) query["daXuLy"] = daXuLy.Value.ToString().ToLower();

            var fullUrl = $"{url}?{query}";
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<T>>>(fullUrl);
            return res ?? new ApiResponse<PagedResult<T>>(500, "Lỗi khi gọi API");
        }

        protected async Task<ApiResponse<List<T>>> GetListAsync<T>(string url)
        {
            var res = await _httpClient.GetFromJsonAsync<ApiResponse<List<T>>>(url);
            return res ?? new ApiResponse<List<T>>(500, "Error");
        }
        protected async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>() 
                   ?? new ApiResponse<T>(500, "Error");
        }
    }
}