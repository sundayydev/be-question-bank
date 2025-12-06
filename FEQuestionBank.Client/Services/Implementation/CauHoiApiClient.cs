using BeQuestionBank.Shared.DTOs.CauHoi;
using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services.Interface;
using System.Net.Http.Json;

namespace FEQuestionBank.Client.Services
{
    public class CauHoiApiClient : BaseApiClient, ICauHoiApiClient
    {
        public CauHoiApiClient(HttpClient httpClient) : base(httpClient)
        {
        }

        public async Task<ApiResponse<IEnumerable<CauHoiDto>>> GetAllAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IEnumerable<CauHoiDto>>>("/api/cauhoi");
            return response ?? ApiResponseFactory.Error<IEnumerable<CauHoiDto>>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<CauHoiWithCauTraLoiDto>> GetByIdAsync(Guid id)
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<CauHoiWithCauTraLoiDto>>($"/api/cauhoi/{id}");
            return response ?? ApiResponseFactory.Error<CauHoiWithCauTraLoiDto>(500, "Lỗi kết nối");
        }

        // Sửa bool -> object
        public async Task<ApiResponse<object>> CreateSingleQuestionAsync(CreateCauHoiWithCauTraLoiDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/single", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<object>> CreateFillingQuestionAsync(CreateCauHoiDienTuDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/dientu", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<object>> CreatePairingQuestionAsync(CreateCauHoiGhepNoiDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/ghepnoi", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<object>> CreateMultipeChoiceQuestionAsync(CreateCauHoiMultipleChoiceDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/multiplechoice", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        // Sửa bool -> object
        public async Task<ApiResponse<object>> CreateGroupQuestionAsync(CreateCauHoiNhomDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/cauhoi/group", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        // Sửa bool -> object (nếu cần)
        public async Task<ApiResponse<object>> UpdateQuestionAsync(Guid id, UpdateCauHoiWithCauTraLoiDto request)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/cauhoi/{id}", request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>()
                   ?? ApiResponseFactory.Error<object>(500, "Lỗi kết nối");
        }

        public async Task<ApiResponse<bool>> DeleteQuestionAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"/api/cauhoi/{id}");
            // Delete trong controller thường trả về ApiResponse.Deleted() (data là null hoặc chuỗi), 
            // nên ApiResponse<bool> có thể vẫn lỗi nếu API không trả về true/false rõ ràng. 
            // An toàn nhất là dùng ApiResponse<object> cho cả Delete.
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>()
                   ?? ApiResponseFactory.Error<bool>(500, "Lỗi kết nối");
        }
    }
}