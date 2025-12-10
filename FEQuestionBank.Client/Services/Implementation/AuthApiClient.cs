using System.Net.Http.Headers;
using BeQuestionBank.Shared.DTOs.Common;
using FEQuestionBank.Client.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;
using BEQuestionBank.Shared.DTOs.user;
using Blazored.LocalStorage;
using FEQuestionBank.Client.Implementation;

namespace FEQuestionBank.Client.Services.Implementation
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private readonly CustomAuthStateProvider _authState;

        public AuthApiClient(HttpClient http, ILocalStorageService localStorage, CustomAuthStateProvider authState)
        {
            _http = http;
            _localStorage = localStorage;
            _authState = authState;
        }

        
        private async Task<HttpResponseMessage> SendWithAutoRefreshAsync(Func<Task<HttpResponseMessage>> request)
        {
            var response = await request();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var refresh = await RefreshTokenFromStorageAsync();
                if (refresh.StatusCode >= 200 && refresh.StatusCode < 300)
                {
                    var newAccess = refresh.Data.GetProperty("accessToken").GetString()!;
                    var newRefresh = refresh.Data.TryGetProperty("refreshToken", out var rt) ? rt.GetString() : null;

                    await _localStorage.SetItemAsync("authToken", newAccess);
                    if (!string.IsNullOrEmpty(newRefresh))
                        await _localStorage.SetItemAsync("refreshToken", newRefresh);

                    await _authState.UpdateStateWithNewToken(newAccess);

                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newAccess);

                    response = await request(); 
                }
            }
            return response;
        }


        public async Task<ApiResponse<JsonElement>> LoginAsync(string tenDangNhap, string matKhau)
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", new { tenDangNhap, matKhau });
            return await HandleResponse(res);
        }

        public async Task<ApiResponse<JsonElement>> RegisterAsync(string tenDangNhap, string matKhau, string? email = null, string vaiTro = "User")
        {
            var res = await _http.PostAsJsonAsync("api/auth/register", new { tenDangNhap, matKhau, email, vaiTro });
            return await HandleResponse(res);
        }

        public async Task<ApiResponse<NguoiDungDto>> GetCurrentUserAsync()
        {
            var res = await SendWithAutoRefreshAsync(() => _http.GetAsync("api/auth/me"));
            return await res.Content.ReadFromJsonAsync<ApiResponse<NguoiDungDto>>();
        }

        public async Task<ApiResponse<JsonElement>> ChangePasswordAsync(string matKhauHienTai, string matKhauMoi)
        {
            var res = await SendWithAutoRefreshAsync(() =>
                _http.PostAsJsonAsync("api/auth/change-password", new { matKhauHienTai, matKhauMoi }));
            return await HandleResponse(res);
        }

        public async Task<ApiResponse<JsonElement>> LogoutAsync()
        {
            var res = await SendWithAutoRefreshAsync(() => _http.PostAsync("api/auth/logout", null));
            return await HandleResponse(res);
        }

        public async Task<ApiResponse<JsonElement>> SendOtpAsync(string email)
        {
            var res = await SendWithAutoRefreshAsync(() =>
                _http.PostAsJsonAsync("api/auth/forgot-password", new { email }));
            return await HandleResponse(res);
        }

        public async Task<ApiResponse<JsonElement>> VerifyOtpAsync(string email, string otp)
        {
            var res = await SendWithAutoRefreshAsync(() =>
                _http.PostAsJsonAsync("api/auth/verify-otp", new { email, otp }));
            return await HandleResponse(res);
        }

        public async Task<ApiResponse<JsonElement>> ResetPasswordAsync(string email, string otp, string matKhauMoi)
        {
            var res = await SendWithAutoRefreshAsync(() =>
                _http.PostAsJsonAsync("api/auth/reset-password", new { email, otp, matKhauMoi }));
            return await HandleResponse(res);
        }

        public async Task<ApiResponse<JsonElement>> RefreshTokenAsync(string refreshToken)
        {
            var res = await _http.PostAsJsonAsync("api/auth/refresh", new { refreshToken });
            return await HandleResponse(res);
        }

        public async Task<ApiResponse<JsonElement>> RefreshTokenFromStorageAsync()
        {
            var refresh = await _localStorage.GetItemAsync<string>("refreshToken");
            if (string.IsNullOrEmpty(refresh))
                return new ApiResponse<JsonElement>(400, "Không có refresh token");
            return await RefreshTokenAsync(refresh);
        }

        private async Task<ApiResponse<JsonElement>> HandleResponse(HttpResponseMessage res)
        {
            var json = await res.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json).RootElement;

            var msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            var data = root.TryGetProperty("data", out var d) ? d : JsonDocument.Parse("{}").RootElement;

            return new ApiResponse<JsonElement>((int)res.StatusCode, msg, data);
        }
    }
}
