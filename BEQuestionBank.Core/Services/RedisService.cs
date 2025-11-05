using StackExchange.Redis;

public class RedisService
{
    private readonly IDatabase _db;
    private const string TOKEN_HASH = "refresh_tokens"; // Hash: userId -> token

    public RedisService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task SetRefreshTokenAsync(string userId, string token, TimeSpan expiry)
    {
        await _db.HashSetAsync(TOKEN_HASH, userId, token);
        await _db.KeyExpireAsync(TOKEN_HASH, expiry); // TTL cho toàn bộ hash
    }

    public async Task<string?> GetRefreshTokenAsync(string userId)
    {
        var value = await _db.HashGetAsync(TOKEN_HASH, userId);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<string?> FindUserIdByTokenAsync(string token)
    {
        var entries = await _db.HashGetAllAsync(TOKEN_HASH);
        foreach (var entry in entries)
        {
            if (entry.Value == token)
                return entry.Name.ToString();
        }
        return null;
    }

    public async Task<bool> RevokeTokenAsync(string userId)
    {
        return await _db.HashDeleteAsync(TOKEN_HASH, userId);
    }

    // Debug
    public async Task<Dictionary<string, string>> GetAllTokensAsync()
    {
        var entries = await _db.HashGetAllAsync(TOKEN_HASH);
        return entries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
    }
}