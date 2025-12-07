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
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrWhiteSpace(token))
                return Anonymous();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var claims = jwt.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
                var identity = new ClaimsIdentity(claims, "jwt");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                return Anonymous();
            }
        }

        // SỬA: DÙNG TryGetProperty + TỰ ĐỘNG TÌM TOKEN
        public async Task MarkUserAsAuthenticated(JsonElement data)
        {
            // LOG TOÀN BỘ JSON ĐỂ DEBUG
            Console.WriteLine("=== LOGIN RESPONSE DATA ===");
            Console.WriteLine(data.GetRawText());
            Console.WriteLine("==============================");

            string? token = null;

            // Danh sách các key có thể chứa token
            var possibleKeys = new[] { "token", "accessToken", "access_token", "jwt", "access", "bearer" };

            foreach (var key in possibleKeys)
            {
                if (data.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    token = prop.GetString();
                    Console.WriteLine($"Token found in key: '{key}' = {token}");
                    break;
                }
            }

            // Nếu không tìm thấy token → báo lỗi rõ
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("ERROR: No token found in response data!");
                throw new InvalidOperationException("Phản hồi từ server không chứa token. Vui lòng kiểm tra backend.");
            }

            // Lưu token + cập nhật auth state
            await _localStorage.SetItemAsync("authToken", token);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var claims = jwt.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
            var identity = new ClaimsIdentity(claims, "jwt");

            NotifyAuthenticationStateChanged(Task.FromResult(
                new AuthenticationState(new ClaimsPrincipal(identity))
            ));
        }

        public async Task MarkUserAsLoggedOut()
        {
            await _localStorage.RemoveItemAsync("authToken");
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