namespace Shared.Security.Jwt;

public interface IJwtBlackListService
{
    Task<bool> CheckBlackListToken(string userId,string requestedJwt);
    Task AddTokenToBlackList(string token,DateTime expiresAt,string userId);
}