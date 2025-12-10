// CustomAuthStateProvider.cs
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace FEQuestionBank.Client.Implementation
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _httpClient;

        public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
        {
            _localStorage = localStorage;
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var accessToken = await _localStorage.GetItemAsync<string>("authToken");
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");

            
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                
                _httpClient.DefaultRequestHeaders.Authorization =
                    !string.IsNullOrWhiteSpace(accessToken)
                        ? new AuthenticationHeaderValue("Bearer", accessToken)
                        : null;

                
                var anonymousIdentity = new ClaimsIdentity(new[] { new Claim("refreshToken", refreshToken) }, "jwt");
                return new AuthenticationState(new ClaimsPrincipal(anonymousIdentity));
            }

  
            if (string.IsNullOrWhiteSpace(accessToken))
                return Anonymous();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(accessToken);
                var identity = new ClaimsIdentity(jwt.Claims, "jwt");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                return Anonymous();
            }
        }
        public async Task UpdateStateWithNewToken(string accessToken)
        {
            await _localStorage.SetItemAsync("authToken", accessToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
        }
        public async Task MarkUserAsAuthenticated(JsonElement data)
        {
            Console.WriteLine("=== LOGIN RESPONSE DATA ===");
            Console.WriteLine(data.GetRawText());

            // 1. LẤY ACCESS TOKEN
            string? accessToken = null;
            if (data.TryGetProperty("accessToken", out var at))
                accessToken = at.GetString();
            else if (data.TryGetProperty("access_token", out at))
                accessToken = at.GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("Không tìm thấy accessToken trong phản hồi từ server!");

            
            string? refreshToken = null;
            if (data.TryGetProperty("refreshToken", out var rt))
                refreshToken = rt.GetString();
            else if (data.TryGetProperty("refresh_token", out rt))
                refreshToken = rt.GetString();

            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new InvalidOperationException("Không tìm thấy refreshToken! Backend phải trả về refreshToken.");

           
            await _localStorage.SetItemAsync("authToken", accessToken);
            await _localStorage.SetItemAsync("refreshToken", refreshToken); 

            Console.WriteLine($"Đã lưu thành công!");
            Console.WriteLine($"AccessToken: {accessToken.Substring(0, 30)}...");
            Console.WriteLine($"RefreshToken: {refreshToken.Substring(0, 30)}...");

            // 4. Cập nhật header cho HttpClient
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // 5. Parse JWT và thông báo đã đăng nhập
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);
            var identity = new ClaimsIdentity(jwt.Claims, "jwt");

            NotifyAuthenticationStateChanged(Task.FromResult(
                new AuthenticationState(new ClaimsPrincipal(identity))
            ));
        }

        public async Task MarkUserAsLoggedOut()
        {
            await _localStorage.RemoveItemsAsync(new[] { "authToken", "refreshToken" }); 
            _httpClient.DefaultRequestHeaders.Authorization = null;
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
        }

        private static AuthenticationState Anonymous() =>
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        public async Task<string?> GetUserIdAsync()
        {
            var authState = await GetAuthenticationStateAsync();
            var user = authState.User;

            return user.FindFirst("sub")?.Value
                   ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

    }
}