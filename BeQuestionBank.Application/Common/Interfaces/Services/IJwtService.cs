namespace BeQuestionBank.Application.Common.Interfaces.Services;

using System;
using System.Security.Claims;
using BeQuestionBank.Domain.Models;

public interface IJwtService
{
    string GenerateAccessToken(string userId, string username, string role);
    string GenerateAccessToken(NguoiDung user);
    string GenerateRefreshToken(string userId, string username);
    (string accessToken, string refreshTokenKey, DateTime expiresAt) GenerateTokenPair(string userId, string username, string role);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
