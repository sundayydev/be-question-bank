using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.Enums;
using System.Net.Http.Json;
using System.Web; // Cần thiết cho HttpUtility

namespace FEQuestionBank.Client.Services
{
    public class FileApiClient : BaseApiClient, IFileApiClient
    {
        public FileApiClient(HttpClient httpClient) : base(httpClient)
        {
        }

        public async Task<ApiResponse<PagedResult<FileDto>>> GetFilesPagedAsync(int page, int pageSize, string? sort, string? search, FileType? fileType)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["page"] = page.ToString();
            query["pageSize"] = pageSize.ToString();

            if (!string.IsNullOrEmpty(sort))
                query["sort"] = sort;

            if (!string.IsNullOrEmpty(search))
                query["search"] = search;

            if (fileType.HasValue)
                query["loaiFile"] = ((int)fileType.Value).ToString();

            // Giả định endpoint API là /api/file
            var url = $"/api/file?{query}";

            try
            {
                var result = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<FileDto>>>(url);
                return result ?? ApiResponseFactory.Error<PagedResult<FileDto>>(500, "Không nhận được phản hồi từ server");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting files: {ex.Message}");
                return ApiResponseFactory.Error<PagedResult<FileDto>>(500, ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> DeleteFileAsync(Guid id)
        {
            try
            {
                // Giả định endpoint xóa là DELETE /api/file/{id}
                var response = await _httpClient.DeleteAsync($"/api/file/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                    return result ?? ApiResponseFactory.Error<bool>(500, "Lỗi khi đọc phản hồi");
                }

                return ApiResponseFactory.Error<bool>((int)response.StatusCode, "Lỗi khi gọi API xóa file");
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<bool>(500, ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> RestoreFileAsync(Guid id)
        {
            try
            {
                // Giả định endpoint khôi phục là POST /api/file/{id}/restore
                var response = await _httpClient.PostAsync($"/api/file/{id}/restore", null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                    return result ?? ApiResponseFactory.Error<bool>(500, "Lỗi khi đọc phản hồi");
                }

                return ApiResponseFactory.Error<bool>((int)response.StatusCode, "Lỗi khi gọi API khôi phục file");
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<bool>(500, ex.Message);
            }
        }

        public async Task<ApiResponse<CauHoiWithCauTraLoiDto>> GetCauHoiByFileIdAsync(Guid fileId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/file/{fileId}/cauhoi");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<CauHoiWithCauTraLoiDto>>();
                    return result ?? ApiResponseFactory.Error<CauHoiWithCauTraLoiDto>(500, "Lỗi khi đọc phản hồi");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponseFactory.Error<CauHoiWithCauTraLoiDto>((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<CauHoiWithCauTraLoiDto>(500, ex.Message);
            }
        }
    }
}