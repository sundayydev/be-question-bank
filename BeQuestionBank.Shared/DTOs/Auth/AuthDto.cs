using System;

namespace BE_CIRRO.Shared.DTOs.Auth;

public class LoginDto
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

public class RegisterDto
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string VaiTro { get; set; } = "User";
}

public class TokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime HanSuDung { get; set; }
    public string LoaiToken { get; set; } = "Bearer";
}

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string MatKhauHienTai { get; set; } = string.Empty;
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

// DTO để xác nhận OTP
public class VerifyOtpDto
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

// DTO để đặt lại mật khẩu
public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public string MatKhauMoi { get; set; } = string.Empty;
}