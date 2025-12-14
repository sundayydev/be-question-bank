using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Tool;
using Microsoft.AspNetCore.Components.Forms;
using FEQuestionBank.Client.Services.Interface;

namespace FEQuestionBank.Client.Services.Implementation;

public class ToolApiClient : IToolApiClient
{
    private readonly HttpClient _httpClient;

    public ToolApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<JsonElement>> UploadImageAsync(IBrowserFile file)
    {
        if (file == null || file.Size == 0)
        {
            return new ApiResponse<JsonElement>
            {
                StatusCode = 400,
                Message = "Chưa chọn file"
            };
        }

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream(10 * 1024 * 1024));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);

        var response = await _httpClient.PostAsync("api/tool/upload-image", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new ApiResponse<JsonElement>
            {
                StatusCode = (int)response.StatusCode,
                Message = responseContent ?? "Lỗi upload hình ảnh"
            };
        }

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Lấy phần data bên trong
            if (jsonDoc.TryGetProperty("data", out JsonElement dataElement) &&
                dataElement.ValueKind == JsonValueKind.Object)
            {
                return new ApiResponse<JsonElement>
                {
                    StatusCode = 200,
                    Message = "Upload thành công",
                    Data = dataElement
                };
            }
            else
            {
                return new ApiResponse<JsonElement>
                {
                    StatusCode = 400,
                    Message = "Không tìm thấy dữ liệu hình ảnh"
                };
            }
        }
        catch (JsonException)
        {
            return new ApiResponse<JsonElement>
            {
                StatusCode = 500,
                Message = "Lỗi phân tích phản hồi từ server"
            };
        }
    }

    public async Task<ApiResponse<JsonElement>> ConvertLatexAsync(ConvertLatexRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tool/convert-latex", request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new ApiResponse<JsonElement>
            {
                StatusCode = (int)response.StatusCode,
                Message = responseContent ?? "Lỗi chuyển đổi LaTeX"
            };
        }

        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (jsonDoc.TryGetProperty("data", out JsonElement dataElement) &&
                dataElement.ValueKind == JsonValueKind.Object)
            {
                return new ApiResponse<JsonElement>
                {
                    StatusCode = 200,
                    Message = "Chuyển đổi thành công",
                    Data = dataElement
                };
            }

            return new ApiResponse<JsonElement>
            {
                StatusCode = 400,
                Message = "Không nhận được dữ liệu hợp lệ"
            };
        }
        catch (JsonException)
        {
            return new ApiResponse<JsonElement>
            {
                StatusCode = 500,
                Message = "Lỗi phân tích phản hồi"
            };
        }
    }
}