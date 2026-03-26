using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class FeatureGate : ComponentBase
{
    [Inject] private IFeatureService FeatureService { get; set; } = null!;

    [Parameter] public string Permission { get; set; } = string.Empty;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? FallbackContent { get; set; }

    private bool _isEnabled;

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(Permission))
        {
            _isEnabled = await FeatureService.IsFeatureEnabledAsync(Permission);
        }
    }
}
