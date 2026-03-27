using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class PermissionGuardedNavLink : ComponentBase
{
    [Inject] private IFeatureService FeatureService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Parameter, EditorRequired] public string Permission { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;
    [Parameter] public string? Icon { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter] public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;
    [Parameter] public bool ShowWhenDisabled { get; set; }

    private bool _featureEnabled;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(Permission))
        {
            _featureEnabled = await FeatureService.IsFeatureEnabledAsync(Permission);
        }
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
