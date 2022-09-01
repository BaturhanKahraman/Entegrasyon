using Amazon.S3.Encryption.Internal;
using Microsoft.Extensions.Caching.Distributed;

namespace Shared.Security.Jwt;

public class RedisJwtBlackList
{
    private const string CacheKey = "jwtblacklist {0}";
    private readonly IDistributedCache _distributedCache;
    public RedisJwtBlackList(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }
    public async Task<bool> CheckBlackListToken(string userId)
    {
        string fullCacheKey = CreateFullCacheKey(userId);
        var oldJwt = await _distributedCache.GetStringAsync(fullCacheKey);
        if (string.IsNullOrEmpty(oldJwt))
            return true;
        await _distributedCache.RemoveAsync(fullCacheKey);
        return false;
    }

    public async Task AddTokenToBlackList(string token,DateTime expiresAt,string userId)
    {
        var fullCacheKey = CreateFullCacheKey(userId);
        await _distributedCache.SetStringAsync(fullCacheKey, token, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = expiresAt
        });
    }

    private static string CreateFullCacheKey(string userId)=> string.Format(CacheKey, userId);
    
}