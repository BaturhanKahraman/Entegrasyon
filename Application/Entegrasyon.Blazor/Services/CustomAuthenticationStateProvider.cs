using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Entegrasyon.Blazor.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _protectedLocalStorage;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(ProtectedLocalStorage protectedLocalStorage)
    {
        _protectedLocalStorage = protectedLocalStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userSessionResult = await _protectedLocalStorage.GetAsync<UserSession>("UserSession");
            var userSession = userSessionResult.Success ? userSessionResult.Value : null;

            if (userSession == null)
            {
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userSession.UserName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.NameIdentifier, userSession.UserId),
                new Claim("Token", userSession.Token)
            }
            .Concat(userSession.Roles.Select(r => new Claim(ClaimTypes.Role, r)))
            .Concat(userSession.Permissions.Select(p => new Claim("Permission", p))),
            "CustomAuth"));

            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }
        catch
        {
            return await Task.FromResult(new AuthenticationState(_anonymous));
        }
    }

    public async Task UpdateAuthenticationState(UserSession? userSession)
    {
        ClaimsPrincipal claimsPrincipal;

        if (userSession != null)
        {
            await _protectedLocalStorage.SetAsync("UserSession", userSession);
            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userSession.UserName),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.NameIdentifier, userSession.UserId),
                new Claim("Token", userSession.Token)
            }
            .Concat(userSession.Roles.Select(r => new Claim(ClaimTypes.Role, r)))
            .Concat(userSession.Permissions.Select(p => new Claim("Permission", p))),
            "CustomAuth"));
        }
        else
        {
            await _protectedLocalStorage.DeleteAsync("UserSession");
            claimsPrincipal = _anonymous;
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }
}

public class UserSession
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
