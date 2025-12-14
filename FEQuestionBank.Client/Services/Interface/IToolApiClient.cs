using System.Text.Json;
using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.Tool;
using Microsoft.AspNetCore.Components.Forms; // để dùng IBrowserFile (tương đương IFormFile ở client)

namespace FEQuestionBank.Client.Services.Interface;

public interface IToolApiClient
{
    Task<ApiResponse<JsonElement>> UploadImageAsync(IBrowserFile file);
    Task<ApiResponse<JsonElement>> ConvertLatexAsync(ConvertLatexRequest request);
}