using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services.Interface;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;

namespace FEQuestionBank.Client.Services.Implementation
{
    public class ImportApiClient : BaseApiClient, IImportApiClient
    {
        private const long MaxFileSize = 100 * 1024 * 1024; // 100MB
        private const long MaxZipSize = 200 * 1024 * 1024; // 200MB

        public ImportApiClient(HttpClient httpClient) : base(httpClient)
        {
        }

        public async Task<ApiResponse<PreviewImportResult>> PreviewWordAsync(IBrowserFile file)
        {
            try
            {
                if (file.Size > MaxFileSize)
                {
                    return ApiResponseFactory.Error<PreviewImportResult>(400, 
                        $"File quá lớn. Kích thước tối đa: {MaxFileSize / 1024 / 1024}MB");
                }

                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream(MaxFileSize));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "File", file.Name);

                var response = await _httpClient.PostAsync("/api/import/preview-word", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<PreviewImportResult>>()
                           ?? ApiResponseFactory.Error<PreviewImportResult>(500, "Lỗi parse response");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponseFactory.Error<PreviewImportResult>((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<PreviewImportResult>(500, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PreviewImportResult>> PreviewZipAsync(IBrowserFile zipFile)
        {
            try
            {
                if (zipFile.Size > MaxZipSize)
                {
                    return ApiResponseFactory.Error<PreviewImportResult>(400, 
                        $"File ZIP quá lớn. Kích thước tối đa: {MaxZipSize / 1024 / 1024}MB");
                }

                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(zipFile.OpenReadStream(MaxZipSize));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(zipFile.ContentType);
                content.Add(fileContent, "ZipFile", zipFile.Name);

                var response = await _httpClient.PostAsync("/api/import/preview-zip", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<PreviewImportResult>>()
                           ?? ApiResponseFactory.Error<PreviewImportResult>(500, "Lỗi parse response");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponseFactory.Error<PreviewImportResult>((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<PreviewImportResult>(500, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ImportResult>> ImportWordAsync(IBrowserFile file, Guid maPhan)
        {
            try
            {
                if (file.Size > MaxFileSize)
                {
                    return ApiResponseFactory.Error<ImportResult>(400, 
                        $"File quá lớn. Kích thước tối đa: {MaxFileSize / 1024 / 1024}MB");
                }

                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream(MaxFileSize));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "File", file.Name);
                content.Add(new StringContent(maPhan.ToString()), "MaPhan");

                var response = await _httpClient.PostAsync("/api/import/word", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<ImportResult>>()
                           ?? ApiResponseFactory.Error<ImportResult>(500, "Lỗi parse response");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponseFactory.Error<ImportResult>((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<ImportResult>(500, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ImportResult>> ImportZipAsync(IBrowserFile zipFile, Guid maPhan)
        {
            try
            {
                if (zipFile.Size > MaxZipSize)
                {
                    return ApiResponseFactory.Error<ImportResult>(400, 
                        $"File ZIP quá lớn. Kích thước tối đa: {MaxZipSize / 1024 / 1024}MB");
                }

                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(zipFile.OpenReadStream(MaxZipSize));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(zipFile.ContentType);
                content.Add(fileContent, "ZipFile", zipFile.Name);
                content.Add(new StringContent(maPhan.ToString()), "MaPhan");

                var response = await _httpClient.PostAsync("/api/import/word-with-media", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<ImportResult>>()
                           ?? ApiResponseFactory.Error<ImportResult>(500, "Lỗi parse response");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponseFactory.Error<ImportResult>((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<ImportResult>(500, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UploadMediaResult>> UploadMediaAsync(List<IBrowserFile> files)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                
                foreach (var file in files)
                {
                    var fileContent = new StreamContent(file.OpenReadStream(MaxFileSize));
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    content.Add(fileContent, "files", file.Name);
                }

                var response = await _httpClient.PostAsync("/api/import/upload-media", content);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<UploadMediaResult>>()
                           ?? ApiResponseFactory.Error<UploadMediaResult>(500, "Lỗi parse response");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponseFactory.Error<UploadMediaResult>((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<UploadMediaResult>(500, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> ClearTempUploadsAsync()
        {
            try
            {
                var response = await _httpClient.DeleteAsync("/api/import/clear-temp-uploads");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<string>>()
                           ?? ApiResponseFactory.Success("Đã xóa thư mục tạm");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponseFactory.Error<string>((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponseFactory.Error<string>(500, $"Lỗi: {ex.Message}");
            }
        }
    }
}
