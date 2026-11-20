using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BEQuestionBank.Core.Helpers;

public class JwtHelper
{
    private readonly IConfiguration _config;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessExpireMinutes;
    private readonly int _refreshExpireDays;

    public JwtHelper(IConfiguration config)
    {
        _config = config;

        var jwt = config.GetSection("JwtSettings");

        _secretKey = jwt["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is missing.");
        _issuer = jwt["Issuer"] ?? "BEQuestionBank";
        _audience = jwt["Audience"] ?? "BEQuestionBank";

        _accessExpireMinutes = int.TryParse(jwt["AccessTokenExpireMinutes"], out var m) ? m : 60;
        _refreshExpireDays = int.TryParse(jwt["RefreshTokenExpireDays"], out var d) ? d : 7;
    }

    // Properties để expose config cho AuthService (nếu cần)
    public int AccessExpireMinutes => _accessExpireMinutes;
    public int RefreshExpireDays => _refreshExpireDays;

    /// <summary>
    /// Generate cả Access Token + Refresh Token Key (random string để lưu Redis)
    /// </summary>
    public (string accessToken, string refreshTokenKey, DateTime expiresAt) GenerateTokenPair(
        string userId, 
        string username, 
        string role)
    {
        var accessToken = GenerateAccessToken(userId, username, role);
        var refreshTokenKey = GenerateSecureRandomString(32); // Random key để lưu Redis
        var expiresAt = DateTime.UtcNow.AddMinutes(_accessExpireMinutes);

        return (accessToken, refreshTokenKey, expiresAt);
    }

    // Access Token
    public string GenerateAccessToken(string userId, string username, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("UserId", userId), // Backward compatibility
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessExpireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Refresh Token (JWT - chứa claims để verify)
    public string GenerateRefreshToken(string userId, string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_refreshExpireDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate random string để dùng làm key trong Redis hoặc refresh token identifier
    /// </summary>
    public static string GenerateSecureRandomString(int byteLength = 32)
    {
        var bytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    // Giải mã token (dùng khi refresh)
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            ValidateLifetime = false 
        };

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    // Dùng cho xác thực thông thường
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
