using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;

namespace FEQuestionBank.Client.Handlers
{
    /// <summary>
    /// DelegatingHandler tự động thêm Bearer token và refresh khi hết hạn
    /// </summary>
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private bool _refreshing = false;

        public AuthTokenHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 1. Thêm access token vào header (nếu có)
            var accessToken = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                Console.WriteLine($"🔐 Đang gửi request với access token (10 ký tự đầu): {accessToken.Substring(0, Math.Min(10, accessToken.Length))}...");
            }
            else
            {
                Console.WriteLine("⚠️ Không có access token trong localStorage");
            }

            // 2. Gửi request
            var response = await base.SendAsync(request, cancellationToken);
            Console.WriteLine($"📡 Response status: {response.StatusCode} cho {request.Method} {request.RequestUri}");

            // 3. Nếu 401 Unauthorized → thử refresh token
            if (response.StatusCode == HttpStatusCode.Unauthorized && !_refreshing)
            {
                Console.WriteLine("🔄 Nhận 401 Unauthorized - Bắt đầu refresh token...");
                _refreshing = true;
                try
                {
                    var refreshed = await TryRefreshTokenAsync(cancellationToken);
                    if (refreshed)
                    {
                        Console.WriteLine("✅ Refresh thành công - Retry request với token mới...");
                        // Lấy token mới và retry request
                        accessToken = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        
                        // Clone và gửi lại request
                        response = await base.SendAsync(request, cancellationToken);
                        Console.WriteLine($"📡 Retry response status: {response.StatusCode}");
                    }
                    else
                    {
                        Console.WriteLine("❌ Refresh thất bại - User sẽ bị logout");
                    }
                }
                finally
                {
                    _refreshing = false;
                }
            }

            return response;
        }

        private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
        {
            try
            {
                var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken", cancellationToken);
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    Console.WriteLine("❌ Không có refresh token trong localStorage");
                    return false;
                }

                Console.WriteLine($"🔄 Đang gọi API refresh với refresh token (10 ký tự đầu): {refreshToken.Substring(0, Math.Min(10, refreshToken.Length))}...");

                // Gọi API refresh (không qua handler này để tránh infinite loop)
                using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5043/") };
                var refreshResponse = await httpClient.PostAsJsonAsync(
                    "api/auth/refresh",
                    new { refreshToken },
                    cancellationToken
                );

                Console.WriteLine($"📡 Refresh API response: {refreshResponse.StatusCode}");

                if (!refreshResponse.IsSuccessStatusCode)
                {
                    // Refresh thất bại → xóa tokens
                    Console.WriteLine("❌ Refresh API thất bại - Xóa tokens");
                    await _localStorage.RemoveItemsAsync(new[] { "authToken", "refreshToken" });
                    return false;
                }

                var json = await refreshResponse.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"📦 Refresh response body: {json}");
                
                var root = JsonDocument.Parse(json).RootElement;
                
                if (!root.TryGetProperty("data", out var data))
                {
                    Console.WriteLine("❌ Response không có property 'data'");
                    return false;
                }

                var newAccessToken = data.GetProperty("accessToken").GetString();
                var newRefreshToken = data.TryGetProperty("refreshToken", out var rt) 
                    ? rt.GetString() 
                    : null;

                if (string.IsNullOrWhiteSpace(newAccessToken))
                {
                    Console.WriteLine("❌ accessToken mới bị null/empty");
                    return false;
                }

                Console.WriteLine($"💾 Đang lưu access token mới (10 ký tự đầu): {newAccessToken.Substring(0, Math.Min(10, newAccessToken.Length))}...");

                // Lưu tokens mới
                await _localStorage.SetItemAsync("authToken", newAccessToken, cancellationToken);
                if (!string.IsNullOrWhiteSpace(newRefreshToken))
                {
                    Console.WriteLine($"💾 Đang lưu refresh token mới (10 ký tự đầu): {newRefreshToken.Substring(0, Math.Min(10, newRefreshToken.Length))}...");
                    await _localStorage.SetItemAsync("refreshToken", newRefreshToken, cancellationToken);
                }

                Console.WriteLine("✅ Auto-refresh token thành công! Đã lưu vào localStorage.");
                
                // Verify đã lưu thành công
                var verifyAccess = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);
                var verifyRefresh = await _localStorage.GetItemAsync<string>("refreshToken", cancellationToken);
                Console.WriteLine($"✓ Verify authToken: {(verifyAccess != null ? verifyAccess.Substring(0, Math.Min(10, verifyAccess.Length)) : "null")}...");
                Console.WriteLine($"✓ Verify refreshToken: {(verifyRefresh != null ? verifyRefresh.Substring(0, Math.Min(10, verifyRefresh.Length)) : "null")}...");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi auto-refresh token: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await _localStorage.RemoveItemsAsync(new[] { "authToken", "refreshToken" });
                return false;
            }
        }
    }
}
