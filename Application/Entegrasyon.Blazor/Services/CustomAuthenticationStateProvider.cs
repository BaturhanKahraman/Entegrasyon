using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Entegrasyon.Blazor.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _protectedLocalStorage;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal? _cachedPrincipal;
    private bool _hasReadFromStorage;

    public CustomAuthenticationStateProvider(ProtectedLocalStorage protectedLocalStorage)
    {
        _protectedLocalStorage = protectedLocalStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_hasReadFromStorage)
            return new AuthenticationState(_cachedPrincipal ?? _anonymous);

        try
        {
            var userSessionResult = await _protectedLocalStorage.GetAsync<UserSession>("UserSession");
            var userSession = userSessionResult.Success ? userSessionResult.Value : null;
            _hasReadFromStorage = true;

            if (userSession == null)
            {
                _cachedPrincipal = _anonymous;
                return new AuthenticationState(_anonymous);
            }

            _cachedPrincipal = BuildClaimsPrincipal(userSession);
            return new AuthenticationState(_cachedPrincipal);
        }
        catch
        {
            _hasReadFromStorage = true;
            _cachedPrincipal = _anonymous;
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task UpdateAuthenticationState(UserSession? userSession)
    {
        ClaimsPrincipal claimsPrincipal;

        if (userSession != null)
        {
            await _protectedLocalStorage.SetAsync("UserSession", userSession);
            claimsPrincipal = BuildClaimsPrincipal(userSession);
        }
        else
        {
            await _protectedLocalStorage.DeleteAsync("UserSession");
            claimsPrincipal = _anonymous;
        }

        _cachedPrincipal = claimsPrincipal;
        _hasReadFromStorage = true;

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    private static ClaimsPrincipal BuildClaimsPrincipal(UserSession session)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, session.UserName),
            new Claim(ClaimTypes.Email, session.Email),
            new Claim(ClaimTypes.NameIdentifier, session.UserId),
            new Claim("Token", session.Token)
        }
        .Concat(session.Roles.Select(r => new Claim(ClaimTypes.Role, r)))
        .Concat(session.Permissions.Select(p => new Claim("Permission", p))),
        "CustomAuth"));
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
