using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;

namespace FEQuestionBank.Client.Services.Implementation
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _http;

        public AuthApiClient(HttpClient http) => _http = http;

        public async Task<ApiResponse<JsonElement>> LoginAsync(string tenDangNhap, string matKhau)
        {
            var payload = new { tenDangNhap, matKhau };
            var response = await _http.PostAsJsonAsync("api/auth/login", payload); 
            return await HandleResponse(response);
        }

        public async Task<ApiResponse<JsonElement>> RegisterAsync(string tenDangNhap, string matKhau, string? email = null, string vaiTro = "User")
        {
            var payload = new { tenDangNhap, matKhau, email, vaiTro };
            var response = await _http.PostAsJsonAsync("api/auth/register", payload); 
            return await HandleResponse(response);
        }

        public async Task<ApiResponse<JsonElement>> GetCurrentUserAsync()
        {
            var response = await _http.GetAsync("api/auth/me");
            return await HandleResponse(response);
        }

        public async Task<ApiResponse<JsonElement>> RefreshTokenAsync(string refreshToken)
        {
            var payload = new { refreshToken };
            var response = await _http.PostAsJsonAsync("api/auth/refresh", payload); 
            return await HandleResponse(response);
        }

        public async Task<ApiResponse<JsonElement>> LogoutAsync()
        {
            var response = await _http.PostAsync("api/auth/logout", null); 
            return await HandleResponse(response);
        }

        public async Task<ApiResponse<JsonElement>> ChangePasswordAsync(string matKhauHienTai, string matKhauMoi)
        {
            var payload = new { matKhauHienTai, matKhauMoi };
            var response = await _http.PostAsJsonAsync("api/auth/change-password", payload); 
            return await HandleResponse(response);
        }

        public async Task<ApiResponse<JsonElement>> SendOtpAsync(string email)
        {
            var payload = new { email };
            var response = await _http.PostAsJsonAsync("api/auth/forgot-password", payload);
            return await HandleResponse(response);
        }

        public async Task<ApiResponse<JsonElement>> VerifyOtpAsync(string email, string otp)
        {
            var payload = new { email, otp };
            var response = await _http.PostAsJsonAsync("api/auth/verify-otp", payload);  
            return await HandleResponse(response);
        }

        public async Task<ApiResponse<JsonElement>> ResetPasswordAsync(string email, string otp, string matKhauMoi)
        {
            var payload = new { email, otp, matKhauMoi };
            var response = await _http.PostAsJsonAsync("api/auth/reset-password", payload); 
            return await HandleResponse(response);
        }

        private static async Task<ApiResponse<JsonElement>> HandleResponse(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var message = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
                var data = root.TryGetProperty("data", out var d) ? d : JsonDocument.Parse("{}").RootElement;

                return new ApiResponse<JsonElement>(
                    (int)response.StatusCode,
                    message,
                    data
                );
            }
            catch
            {
                return new ApiResponse<JsonElement>((int)response.StatusCode, "Lỗi parse JSON", default);
            }
        }
    }
}