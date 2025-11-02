using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.NguoiDung;
using BeQuestionBank.Shared.Enums;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using BEQuestionBank.Core.Helpers;
using BEQuestionBank.Domain.Interfaces.Repo;

namespace BEQuestionBank.Core.Services;

public class AuthService
{
    private readonly INguoiDungRepository _nguoiDungRepo;
    private readonly JwtHelper _jwt;
    private readonly IConfiguration _config;

    public AuthService(
        INguoiDungRepository nguoiDungRepo,
        JwtHelper jwt,
        IConfiguration config)
    {
        _nguoiDungRepo = nguoiDungRepo;
        _jwt = jwt;
        _config = config;
    }

    public async Task<TokenResponse?> LoginAsync(LoginDto request)
    {
        var user = await _nguoiDungRepo.GetByUsernameAsync(request.TenDangNhap);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.MatKhau, user.MatKhau))
            return null;

        if (user.BiKhoa)
            return null;

        var role = user.VaiTro == EnumRole.Admin ? "Admin" : "User";
        var accessToken = _jwt.GenerateAccessToken(user.MaNguoiDung.ToString(), user.TenDangNhap, role);
        var refreshToken = GenerateRefreshToken();

        // Cập nhật thời gian đăng nhập cuối
        user.NgayDangNhapCuoi = DateTime.UtcNow;
        await _nguoiDungRepo.UpdateAsync(user);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = int.Parse(_config["JwtSettings:AccessTokenExpireMinutes"] ?? "60") * 60
        };
    }

    public async Task<TokenResponse?> RefreshTokenAsync(RefreshRequest request)
    {
        // Chỉ cần validate JWT refresh token nếu bạn mã hoá nó theo chuẩn
        var principal = _jwt.GetPrincipalFromExpiredToken(request.RefreshToken);
        if (principal == null) return null;

        var userId = principal.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        if (userId == null) return null;

        var user = await _nguoiDungRepo.GetByIdAsync(Guid.Parse(userId));
        if (user == null || user.BiKhoa) return null;

        var role = user.VaiTro == EnumRole.Admin ? "Admin" : "User";
        var newAccessToken = _jwt.GenerateAccessToken(user.MaNguoiDung.ToString(), user.TenDangNhap, role);
        var newRefreshToken = GenerateRefreshToken();

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = int.Parse(_config["JwtSettings:AccessTokenExpireMinutes"] ?? "60") * 60
        };
    }

    public Task<bool> LogoutAsync(string userId)
    {
        // Nếu không lưu refresh token, logout chỉ là thao tác client-side
        return Task.FromResult(true);
    }

    public async Task<NguoiDung?> RegisterAsync(RegisterDto request)
    {
        if (await _nguoiDungRepo.GetByUsernameAsync(request.TenDangNhap) != null)
            return null;

        var user = new NguoiDung
        {
            MaNguoiDung = Guid.NewGuid(),
            TenDangNhap = request.TenDangNhap,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(request.MatKhau),
            HoTen = request.HoTen ?? string.Empty,
            Email = request.Email,
            VaiTro = request.VaiTro,
            BiKhoa = false
        };

        await _nguoiDungRepo.AddAsync(user);
        return user;
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
