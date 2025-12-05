using BE_CIRRO.Core.Services;

using BE_CIRRO.Shared.DTOs.Auth;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.Enums;
using BEQuestionBank.Core.Services;
using BEQuestionBank.Shared.DTOs.user;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BE_CIRRO.Core.Services;

public class AuthService
{
    private readonly NguoiDungService _userService;
    private readonly KhoaService _khoaService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IDatabase _redisDb;
    private const string RefreshTokenPrefix = "refresh_token:";
    private const string OtpPrefix = "otp:";

    public AuthService(NguoiDungService userService, IConfiguration configuration, ILogger<AuthService> logger, IConnectionMultiplexer redis)
    {
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
        _redisDb = redis.GetDatabase();
    }

    private string GetRedisKey(string refreshToken) => $"{RefreshTokenPrefix}{refreshToken}";
    private string GetOtpKey(string email) => $"{OtpPrefix}{email}";

    // ĐĂNG KÝ
    public async Task<NguoiDungDto?> RegisterAsync(RegisterDto dto)
    {
        try
        {
            var existingUser = await _userService.GetByUsernameAsync(dto.TenDangNhap);
            if (existingUser != null)
            {
                _logger.LogWarning("Tên đăng nhập {TenDangNhap} đã tồn tại", dto.TenDangNhap);
                return null;
            }

            var newUser = new NguoiDung
            {
                MaNguoiDung = Guid.NewGuid(),
                TenDangNhap = dto.TenDangNhap,
                MatKhau = dto.MatKhau,
                Email = dto.Email,
                VaiTro = Enum.Parse<EnumRole>(dto.VaiTro, true),
                BiKhoa = false
            };

            var user = await _userService.CreateAsync(newUser);
            return user.Adapt<NguoiDungDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng ký người dùng {TenDangNhap}", dto.TenDangNhap);
            throw;
        }
    }

    // ĐĂNG NHẬP
    public async Task<TokenDto?> LoginAsync(LoginDto dto)
    {
        try
        {
            var user = await _userService.GetByUsernameAsync(dto.TenDangNhap);
            if (user == null)
            {
                _logger.LogWarning("Đăng nhập thất bại: Không tìm thấy {TenDangNhap}", dto.TenDangNhap);
                return null;
            }

            if (!VerifyPassword(dto.MatKhau, user.MatKhau))
            {
                _logger.LogWarning("Mật khẩu không đúng cho {TenDangNhap}", dto.TenDangNhap);
                return null;
            }
            user.NgayDangNhapCuoi = DateTime.UtcNow; 
            await _userService.UpdateAsync(user); 
            var tokenDto = await GenerateTokenAsync(user);
            _logger.LogInformation("Đăng nhập thành công: {TenDangNhap}", dto.TenDangNhap);
            return tokenDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi đăng nhập cho {TenDangNhap}", dto.TenDangNhap);
            throw;
        }
    }

    // LẤY USER HIỆN TẠI
    public async Task<NguoiDungDto?> GetCurrentUserAsync(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("ID người dùng không hợp lệ: {UserId}", userId);
                return null;
            }

            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return null;

            // var userDto = user.Adapt<NguoiDungDto>();
            //
            //
            // return userDto;
            return new NguoiDungDto
            {
                MaNguoiDung = user.MaNguoiDung,
                TenDangNhap = user.TenDangNhap,
                HoTen = user.HoTen,
                Email = user.Email,
                VaiTro = user.VaiTro,
                BiKhoa = user.BiKhoa,
                MaKhoa = user.MaKhoa,
                TenKhoa = user.Khoa?.TenKhoa,
                NgayTao = user.NgayTao,
                NgayCapNhat = user.NgayCapNhat,
                NgayDangNhapCuoi = user.NgayDangNhapCuoi
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi lấy thông tin user ID {UserId}", userId);
            throw;
        }
    }

    // LÀM MỚI TOKEN
    public async Task<TokenDto?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var redisKey = GetRedisKey(refreshToken);
            var userIdValue = await _redisDb.StringGetAsync(redisKey);

            if (userIdValue.IsNullOrEmpty)
            {
                _logger.LogWarning("Refresh token không hợp lệ hoặc đã hết hạn");
                return null;
            }

            var userId = Guid.Parse(userIdValue);
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                await _redisDb.KeyDeleteAsync(redisKey);
                _logger.LogWarning("Không tìm thấy user với ID từ refresh token: {UserId}", userId);
                return null;
            }

            var newToken = await GenerateTokenAsync(user);
            await _redisDb.KeyDeleteAsync(redisKey); // Token Rotation

            _logger.LogInformation("Làm mới token thành công cho user ID: {UserId}", userId);
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi làm mới token");
            throw;
        }
    }

    // ĐĂNG XUẤT
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        try
        {
            var redisKey = GetRedisKey(refreshToken);
            var deleted = await _redisDb.KeyDeleteAsync(redisKey);

            if (deleted)
                _logger.LogInformation("Đăng xuất thành công (token đã bị thu hồi)");
            else
                _logger.LogWarning("Đăng xuất thất bại: token không tồn tại hoặc đã hết hạn");

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đăng xuất");
            throw;
        }
    }

    // ĐỔI MẬT KHẨU
    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy user ID: {UserId}", userId);
                return false;
            }

            if (!VerifyPassword(currentPassword, user.MatKhau))
            {
                _logger.LogWarning("Mật khẩu hiện tại không đúng cho user ID: {UserId，是}", userId);
                return false;
            }

            user.MatKhau = HashPassword(newPassword);
            await _userService.UpdateAsync(userId, user);

            _logger.LogInformation("Đổi mật khẩu thành công cho user ID: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi đổi mật khẩu user ID: {UserId}", userId);
            throw;
        }
    }

    // TẠO TOKEN
    private async Task<TokenDto> GenerateTokenAsync(NguoiDung user)
    {
        var jwt = _configuration.GetSection("JwtSettings");
        var secretKey = jwt["SecretKey"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
        var issuer = jwt["Issuer"] ?? "BE_CIRRO";
        var audience = jwt["Audience"] ?? "BE_CIRRO_Users";
        var expiryMinutes = int.Parse(jwt["ExpiryMinutes"] ?? "60");
        var refreshExpiryDays = int.Parse(jwt["RefreshExpiryDays"] ?? "7");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()), // <-- THÊM DÒNG NÀY
            new Claim(ClaimTypes.Name, user.TenDangNhap),
            new Claim(ClaimTypes.Role, user.VaiTro.ToString()),
            new Claim("UserId", user.MaNguoiDung.ToString()), // có thể bỏ dòng này nếu muốn sạch
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        var redisKey = GetRedisKey(refreshToken);
        var stored = await _redisDb.StringSetAsync(
            redisKey,
            user.MaNguoiDung.ToString(),
            TimeSpan.FromDays(refreshExpiryDays)
        );

        if (!stored)
        {
            _logger.LogError("Lỗi lưu refresh token vào Redis cho user {MaNguoiDung}", user.MaNguoiDung);
            throw new Exception("Lỗi hệ thống: Không thể tạo phiên đăng nhập.");
        }

        return new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            HanSuDung = DateTime.UtcNow.AddMinutes(expiryMinutes),
            LoaiToken = "Bearer"
        };
    }

    private string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    private bool VerifyPassword(string password, string hashed) => BCrypt.Net.BCrypt.Verify(password, hashed);

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwt = _configuration.GetSection("JwtSettings");
            var secretKey = jwt["SecretKey"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
            var issuer = jwt["Issuer"] ?? "BE_CIRRO";
            var audience = jwt["Audience"] ?? "BE_CIRRO_Users";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var handler = new JwtSecurityTokenHandler();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return handler.ValidateToken(token, parameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xác thực JWT token");
            return null;
        }
    }

    // DEBUG: Kiểm tra token
    public async Task<bool> IsRefreshTokenValid(string refreshToken)
        => await _redisDb.KeyExistsAsync(GetRedisKey(refreshToken));

    public async Task<string?> GetRefreshTokenInfo(string refreshToken)
    {
        var value = await _redisDb.StringGetAsync(GetRedisKey(refreshToken));
        return value.IsNullOrEmpty ? null : $"UserId: {value}";
    }

    // === QUÊN MẬT KHẨU ===
    public async Task<bool> SendOtpAsync(ForgotPasswordDto dto)
    {
        try
        {
            var user = await _userService.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Không tìm thấy email: {Email}", dto.Email);
                return false;
            }

            var otp = GenerateOtp();
            var key = GetOtpKey(dto.Email);
            var stored = await _redisDb.StringSetAsync(key, otp, TimeSpan.FromMinutes(5));

            if (!stored)
            {
                _logger.LogError("Lỗi lưu OTP vào Redis cho {Email}", dto.Email);
                throw new Exception("Lỗi hệ thống: Không thể tạo OTP.");
            }

            await SendOtpEmailAsync(dto.Email, otp);
            _logger.LogInformation("Đã gửi OTP đến {Email}", dto.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gửi OTP cho {Email}", dto.Email);
            throw;
        }
    }

    public async Task<bool> VerifyOtpAsync(VerifyOtpDto dto)
    {
        try
        {
            var key = GetOtpKey(dto.Email);
            var stored = await _redisDb.StringGetAsync(key);

            if (stored.IsNullOrEmpty || stored != dto.Otp)
            {
                _logger.LogWarning("OTP không hợp lệ cho {Email}", dto.Email);
                return false;
            }

            _logger.LogInformation("Xác nhận OTP thành công: {Email}", dto.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xác nhận OTP cho {Email}", dto.Email);
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            var otpKey = GetOtpKey(dto.Email);
            var storedOtp = await _redisDb.StringGetAsync(otpKey);

            // Lấy user + kiểm tra OTP (chỉ 1 lần query)
            var user = await _userService.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || storedOtp.IsNullOrEmpty || storedOtp != dto.Otp)
            {
                if (storedOtp.HasValue) await _redisDb.KeyDeleteAsync(otpKey);
                return false;
            }

            // Cập nhật trực tiếp trên instance đã có
            user.MatKhau = HashPassword(dto.MatKhauMoi);
            await _userService.UpdateAsync(user); // <-- DÙNG OVERLOAD NÀY

            await _redisDb.KeyDeleteAsync(otpKey);
            _logger.LogInformation("Reset password thành công: {Email}", dto.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi reset password: {Email}", dto.Email);
            return false;
        }
    }

    private string GenerateOtp()
    {
        return new Random().Next(100000, 999999).ToString();
    }

    private async Task SendOtpEmailAsync(string email, string otp)
    {
        try
        {
            var smtp = _configuration.GetSection("SmtpSettings");
            var client = new SmtpClient
            {
                Host = smtp["Host"] ?? "smtp.gmail.com",
                Port = int.Parse(smtp["Port"] ?? "587"),
                EnableSsl = bool.Parse(smtp["EnableSsl"] ?? "true"),
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtp["Username"], smtp["Password"])
            };

            var mail = new MailMessage
            {
                From = new MailAddress(smtp["FromEmail"] ?? "no-reply@becirro.com", "BE_CIRRO"),
                Subject = "Mã OTP đặt lại mật khẩu",
                Body = $@"<h2>Mã OTP của bạn</h2>
                         <p><strong>{otp}</strong></p>
                         <p>Hết hạn sau <strong>5 phút</strong>.</p>",
                IsBodyHtml = true
            };
            mail.To.Add(email);

            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gửi email OTP đến {Email}", email);
            throw new Exception("Không thể gửi email OTP.", ex);
        }
    }
}