namespace Shared.Security.Jwt;

public record AccessToken
{
    public string Token { get; init; }
    public DateTime ExpiresAt { get; init; }
}