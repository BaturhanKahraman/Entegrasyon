namespace Shared.Security.Jwt;

public interface IJwtBlackListService
{
    Task<bool> CheckBlackListToken(string userId);
    Task AddTokenToBlackList(string token,DateTime expiresAt,string userId);
}