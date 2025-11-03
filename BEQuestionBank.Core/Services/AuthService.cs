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
    private readonly RedisService _redis;

    public AuthService(
        INguoiDungRepository nguoiDungRepo,
        JwtHelper jwt,
        IConfiguration config,
        RedisService redis)
    {
        _nguoiDungRepo = nguoiDungRepo;
        _jwt = jwt;
        _config = config;
        _redis = redis;
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
        //  TTL cho refresh token
        var refreshDays = int.Parse(_config["JwtSettings:RefreshTokenExpireDays"] ?? "7");
        var expiry = TimeSpan.FromDays(refreshDays);
        
        await _redis.SetRefreshTokenAsync(user.MaNguoiDung.ToString(), refreshToken, expiry);

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
        var principal = _jwt.GetPrincipalFromExpiredToken(request.RefreshToken);
        if (principal == null) return null;

        var userId = principal.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        if (userId == null) return null;

        var user = await _nguoiDungRepo.GetByIdAsync(Guid.Parse(userId));
        if (user == null || user.BiKhoa) return null;

        //  Kiểm tra token trong Redis
        var savedToken = await _redis.GetRefreshTokenAsync(userId);
        if (savedToken != request.RefreshToken)
            return null; // Refresh token không khớp → từ chối

        var role = user.VaiTro == EnumRole.Admin ? "Admin" : "User";
        var newAccessToken = _jwt.GenerateAccessToken(user.MaNguoiDung.ToString(), user.TenDangNhap, role);
        var newRefreshToken = GenerateRefreshToken();

        // 🔁 Ghi đè refresh token mới trong Redis
        var refreshDays = int.Parse(_config["JwtSettings:RefreshTokenExpireDays"] ?? "7");
        await _redis.SetRefreshTokenAsync(userId, newRefreshToken, TimeSpan.FromDays(refreshDays));

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = int.Parse(_config["JwtSettings:AccessTokenExpireMinutes"] ?? "60") * 60
        };
    }


    public async  Task<bool> LogoutAsync(string userId)
    {
        // Nếu không lưu refresh token, logout chỉ là thao tác client-side
        return await _redis.RevokeTokenAsync(userId);
       // return Task.FromResult(true);
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
