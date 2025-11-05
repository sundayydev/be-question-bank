using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FEQuestionBank.Client.Services;

// Phải kế thừa từ AuthenticationStateProvider
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    // Key để lưu/đọc token
    public const string AccessTokenKey = "accessToken";

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Phương thức này được Blazor gọi khi khởi động
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var accessToken = await _localStorage.GetItemAsync<string>(AccessTokenKey);

        if (string.IsNullOrEmpty(accessToken))
        {
            // Chưa đăng nhập
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // Đã đăng nhập -> set token vào header cho các request sau
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        // Tạo trạng thái auth từ token
        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(ParseClaimsFromJwt(accessToken), "jwtAuth")
        );
        
        return new AuthenticationState(claimsPrincipal);
    }

    /// <summary>
    /// Gọi khi đăng nhập thành công
    /// </summary>
    public async Task NotifyUserAuthentication(string token)
    {
        var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwtAuth");
        var user = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(user));
        
        NotifyAuthenticationStateChanged(authState);
    }

    /// <summary>
    /// Gọi khi đăng xuất
    /// </summary>
    public void NotifyUserLogout()
    {
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        var authState = Task.FromResult(new AuthenticationState(user));
        
        NotifyAuthenticationStateChanged(authState);
    }

    // --- Hàm helper để đọc JWT ---
    
    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            // Lấy roles (nếu backend trả về)
            if (keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles))
            {
                var rolesArray = roles.ToString().Trim().Trim('[', ']').Split(',');
                foreach (var role in rolesArray)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Trim().Trim('"')));
                }
            }
            
            // Lấy các claims khác (như nameid, email, v.v.)
            claims.AddRange(keyValuePairs.Select(kvp => 
                new Claim(kvp.Key, kvp.Value.ToString())
            ));
        }
        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}