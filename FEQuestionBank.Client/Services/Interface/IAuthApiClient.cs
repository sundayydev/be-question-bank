using BeQuestionBank.Shared.DTOs.Common;
using System.Text.Json;

namespace FEQuestionBank.Client.Services.Interface
{
    public interface IAuthApiClient
    {
        Task<ApiResponse<JsonElement>> LoginAsync(string tenDangNhap, string matKhau);
        Task<ApiResponse<JsonElement>> RegisterAsync(string tenDangNhap, string matKhau, string? email = null, string vaiTro = "User");
        Task<ApiResponse<JsonElement>> GetCurrentUserAsync();
        Task<ApiResponse<JsonElement>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponse<JsonElement>> LogoutAsync();
        Task<ApiResponse<JsonElement>> ChangePasswordAsync(string matKhauHienTai, string matKhauMoi);
        Task<ApiResponse<JsonElement>> SendOtpAsync(string email);
        Task<ApiResponse<JsonElement>> VerifyOtpAsync(string email, string otp);
        Task<ApiResponse<JsonElement>> ResetPasswordAsync(string email, string otp, string matKhauMoi);
    }
}
