// // Tạo file này (ví dụ: FEQuestionBank.Client.Services/AuthApiClient.cs)
// using Blazored.LocalStorage;
// using BeQuestionBank.Shared.DTOs.NguoiDung; // Sử dụng DTOs của bạn
// using Microsoft.AspNetCore.Components.Authorization;
// using System.Net.Http;
// using System.Net.Http.Json;
// using System.Net.Http.Headers;
// using System.Threading.Tasks;
// using System.Text.Json; // Cần cho việc parse JWT
//
// namespace FEQuestionBank.Client.Services;
//
// public class AuthApiClient
// {
//     private readonly HttpClient _httpClient;
//     private readonly ILocalStorageService _localStorage;
//     private readonly AuthenticationStateProvider _authenticationStateProvider;
//
//     // Key để lưu/đọc token
//     private const string AccessTokenKey = "accessToken";
//     private const string RefreshTokenKey = "refreshToken";
//
//     public AuthApiClient(HttpClient httpClient,
//                          ILocalStorageService localStorage,
//                          AuthenticationStateProvider authenticationStateProvider)
//     {
//         _httpClient = httpClient;
//         _localStorage = localStorage;
//         _authenticationStateProvider = authenticationStateProvider;
//     }
//
//     /// <summary>
//     /// Gọi API Login, sử dụng LoginDto của bạn
//     /// </summary>
//     public async Task<bool> Login(LoginDto loginDto)
//     {
//         var response = await _httpClient.PostAsJsonAsync("api/Auth/login", loginDto);
//
//         if (!response.IsSuccessStatusCode)
//             return false;
//
//         // Sử dụng TokenResponse đã fix
//         var authResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
//         if (authResponse == null) return false;
//
//         // Lưu token vào Local Storage
//         await _localStorage.SetItemAsync(AccessTokenKey, authResponse.AccessToken);
//         await _localStorage.SetItemAsync(RefreshTokenKey, authResponse.RefreshToken);
//
//         // Thông báo cho CustomAuthenticationStateProvider
//         await ((CustomAuthenticationStateProvider)_authenticationStateProvider)
//             .NotifyUserAuthentication(authResponse.AccessToken);
//
//         // Đặt header cho các request tiếp theo
//         _httpClient.DefaultRequestHeaders.Authorization =
//             new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
//
//         return true;
//     }
//
//     /// <summary>
//     /// Gọi API Logout
//     /// </summary>
//     public async Task Logout()
//     {
//         // Lấy UserID từ token để gọi API backend
//         var accessToken = await _localStorage.GetItemAsync<string>(AccessTokenKey);
//         var userId = GetUserIdFromToken(accessToken);
//
//         if (!string.IsNullOrEmpty(userId))
//         {
//             // Backend của bạn yêu cầu [FromBody] string
//             await _httpClient.PostAsJsonAsync("api/Auth/logout", userId);
//         }
//
//         // Dọn dẹp phía client
//         await _localStorage.RemoveItemAsync(AccessTokenKey);
//         await _localStorage.RemoveItemAsync(RefreshTokenKey);
//
//         // Thông báo cho Blazor
//         ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserLogout();
//         _httpClient.DefaultRequestHeaders.Authorization = null;
//     }
//     
//     /// <summary>
//     /// Gọi API Refresh, sử dụng RefreshRequest của bạn
//     /// </summary>
//     public async Task<string> RefreshTokenAsync()
//     {
//         var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenKey);
//         if (string.IsNullOrEmpty(refreshToken)) return null;
//     
//         // Sử dụng DTO RefreshRequest bạn đã cung cấp (chỉ chứa RefreshToken)
//         var refreshRequest = new RefreshRequest { RefreshToken = refreshToken };
//     
//         var response = await _httpClient.PostAsJsonAsync("api/Auth/refresh", refreshRequest);
//     
//         if (!response.IsSuccessStatusCode)
//         {
//             await Logout(); // Đăng xuất nếu refresh token hết hạn
//             return null;
//         }
//     
//         var authResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
//         await _localStorage.SetItemAsync(AccessTokenKey, authResponse.AccessToken);
//         await _localStorage.SetItemAsync(RefreshTokenKey, authResponse.RefreshToken);
//     
//         _httpClient.DefaultRequestHeaders.Authorization = 
//             new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
//             
//         return authResponse.AccessToken;
//     }
//
//     // --- Hàm helper để đọc UserID từ JWT (dùng cho Logout) ---
//     private string GetUserIdFromToken(string token)
//     {
//         if (string.IsNullOrEmpty(token)) return null;
//         try
//         {
//             var parts = token.Split('.');
//             if (parts.Length != 3) return null;
//
//             var payload = parts[1];
//             var payloadJson = Base64UrlDecode(payload);
//             var claims = JsonDocument.Parse(payloadJson);
//
//             // Tìm claim "nameid" (thường là UserId) hoặc "sub"
//             if (claims.RootElement.TryGetProperty("nameid", out var userIdClaim))
//             {
//                 return userIdClaim.GetString();
//             }
//             if (claims.RootElement.TryGetProperty("sub", out var subClaim))
//             {
//                 return subClaim.GetString();
//             }
//             return null;
//         }
//         catch { return null; }
//     }
//
//     private string Base64UrlDecode(string input)
//     {
//         var output = input.Replace('-', '+').Replace('_', '/');
//         switch (output.Length % 4)
//         {
//             case 0: break;
//             case 2: output += "=="; break;
//             case 3: output += "="; break;
//             default: throw new System.ArgumentException("Illegal base64url string!");
//         }
//         var converted = Convert.FromBase64String(output);
//         return System.Text.Encoding.UTF8.GetString(converted);
//     }
// }