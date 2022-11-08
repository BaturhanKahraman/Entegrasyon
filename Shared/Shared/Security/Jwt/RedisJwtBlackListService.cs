using Microsoft.Extensions.Caching.Distributed;

namespace Shared.Security.Jwt;

public class RedisJwtBlackListService: IJwtBlackListService
{
    private const string CacheKey = "jwtblacklist {0}";
    private readonly IDistributedCache _distributedCache;
    public RedisJwtBlackListService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }
    public async Task<bool> CheckBlackListToken(string userId,string requestedJwt)
    {
        string fullCacheKey = CreateFullCacheKey(userId);
        var oldJwt = await _distributedCache.GetStringAsync(fullCacheKey);
        if (string.IsNullOrEmpty(oldJwt) || !string.Equals(requestedJwt,oldJwt))
            return true;
        await _distributedCache.RemoveAsync(fullCacheKey);
        return false;
    }

    public async Task AddTokenToBlackList(string token,DateTime expiresAt,string userId)
    {
        //TODO
        //eklenirken eskini ezmemesi lazım.
        var fullCacheKey = CreateFullCacheKey(userId);
        await _distributedCache.SetStringAsync(fullCacheKey, token, new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = expiresAt
        });
    }

    private static string CreateFullCacheKey(string userId)=> string.Format(CacheKey, userId);
    
}