namespace BeQuestionBank.Application.Common.Interfaces.Services;

using System;
using System.Threading.Tasks;

public interface IRedisCacheService
{
    Task SetRefreshTokenAsync(string userId, string token, TimeSpan expiry);
    Task<string?> GetRefreshTokenAsync(string userId);
    Task<string?> FindUserIdByTokenAsync(string token);
    Task<bool> RevokeTokenAsync(string userId);
}
