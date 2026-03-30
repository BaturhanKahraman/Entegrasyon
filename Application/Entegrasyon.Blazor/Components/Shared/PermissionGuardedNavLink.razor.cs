using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class PermissionGuardedNavLink : ComponentBase
{
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    [Parameter, EditorRequired] public string Permission { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;
    [Parameter] public string? Icon { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter] public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;
    [Parameter] public bool ShowWhenDisabled { get; set; }

    private bool _hasPermission;

    protected override async Task OnParametersSetAsync()
    {
        if (AuthStateTask is null) return;

        var authState = await AuthStateTask;
        var user = authState.User;

        // Admin her zaman gecer — authorization pipeline'a girmeye gerek yok
        _hasPermission = user.IsInRole("Admin")
            || user.HasClaim("Permission", Permission);
    }

    private async Task OnDisabledClick()
    {
        var parameters = new DialogParameters<UpgradePromptDialog>
        {
            { x => x.FeatureName, Title }
        };

        await DialogService.ShowAsync<UpgradePromptDialog>(
            "Paket Yukseltme",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
    }
}
