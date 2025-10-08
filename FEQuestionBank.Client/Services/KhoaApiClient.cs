using System.Net.Http.Json;
using FEQuestionBank.Client.Share;

namespace FEQuestionBank.Client.Services
{
    public class KhoaApiClient : IKhoaApiClient
    {
        private readonly HttpClient _httpClient;
        public KhoaApiClient(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<List<KhoaDto>> GetKhoasAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<KhoaDto>>>("api/khoa");
            return response?.Data ?? new List<KhoaDto>();
        }
    }

}