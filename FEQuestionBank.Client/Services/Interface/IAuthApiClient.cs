using BeQuestionBank.Shared.DTOs.Common;
using BeQuestionBank.Shared.DTOs.NguoiDung;

namespace FEQuestionBank.Client.Services;

public interface IAuthApiClient
{
    Task<ApiResponse<TokenResponse>> LoginAsync(LoginDto model);
    Task<ApiResponse<TokenResponse>> RefreshTokenAsync(RefreshRequest model);
    Task<ApiResponse<bool>> LogoutAsync(string userId);
}