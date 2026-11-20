namespace BeQuestionBank.Application.Common.Interfaces.Services;

using System;
using System.Security.Claims;
using BeQuestionBank.Domain.Models;

public interface IJwtService
{
    string GenerateAccessToken(NguoiDung user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
