using Entegrasyon.Blazor.Services;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Auth;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Auth;

public partial class Login
{
    [Inject] private IAuthService AuthService { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private ITenantContext TenantContext { get; set; } = null!;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isValid;
    private bool _isLoading;

    private async Task LoginAsync()
    {
        if (!_isValid) return;

        _isLoading = true;
        try
        {
            var result = await AuthService.LoginAsync(_username, _password);
            if (result.Success)
            {
                if (result is SuccessDataResult<UserLoginSuccessDto> successResult)
                {
                    var user = successResult.Data;
                    var permissions = new List<string>();

                    if (user.Roles != null)
                    {
                        foreach (var role in user.Roles)
                        {
                            if (role.RoleClaims != null)
                            {
                                permissions.AddRange(role.RoleClaims
                                    .Where(rc => !string.IsNullOrEmpty(rc.Permission))
                                    .Select(rc => rc.Permission!)
                                );
                            }
                        }
                    }

                    var session = new UserSession
                    {
                        UserId = user.Id.ToString(),
                        UserName = user.Username,
                        Email = $"{user.Name} {user.Surname}",
                        Token = Guid.NewGuid().ToString(),
                        TenantId = TenantContext.IsInitialized ? TenantContext.TenantId : 1,
                        Roles = user.Roles?.Select(r => r.Name).ToList() ?? [],
                        Permissions = permissions.Distinct().ToList()
                    };

                    var customAuthStateProvider = (CustomAuthenticationStateProvider)AuthStateProvider;
                    await customAuthStateProvider.UpdateAuthenticationState(session);

                    NavigationManager.NavigateTo("/");
                }
                else if (result is SuccessDataResult<LoginNewPasswordDto> newPasswordResult)
                {
                    NavigationManager.NavigateTo($"/auth/reset-password?userId={newPasswordResult.Data.UserId}");
                }
                else
                {
                    Snackbar.Add(result.Message ?? "", Severity.Error);
                }
            }
            else
            {
                Snackbar.Add(result.Message ?? "", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Bir hata oluştu: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }
}
