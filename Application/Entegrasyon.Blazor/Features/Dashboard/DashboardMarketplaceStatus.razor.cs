using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Dashboard;

public partial class DashboardMarketplaceStatus
{
    [Inject] private IDashboardManager DashboardManager { get; set; } = null!;
    [Inject] private ITenantContext TenantContext { get; set; } = null!;

    private List<MarketplaceStatusViewModel> _marketplaceStatuses = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        if (!TenantContext.IsInitialized) return;
        await LoadAsync();
    }

    public async Task RefreshAsync()
    {
        _loading = true;
        StateHasChanged();
        await LoadAsync();
        StateHasChanged();
    }

    private async Task LoadAsync()
    {
        var data = await DashboardManager.GetMarketplaceStatusesAsync();
        _marketplaceStatuses = data
            .Select(m => new MarketplaceStatusViewModel(
                Name: m.Name,
                Icon: GetMarketplaceIcon(m.Name),
                Color: GetMarketplaceColor(m.Name),
                SyncedCount: m.SyncedCount,
                PendingCount: m.PendingCount,
                FailedCount: m.FailedCount))
            .ToList();
        _loading = false;
    }

    private static string GetMarketplaceIcon(string name) => name.ToLowerInvariant() switch
    {
        "trendyol" => Icons.Material.Filled.Store,
        "hepsiburada" => Icons.Material.Filled.ShoppingCart,
        "n11" => Icons.Material.Filled.Storefront,
        "amazon" => Icons.Material.Filled.LocalMall,
        _ => Icons.Material.Filled.Store
    };

    private static Color GetMarketplaceColor(string name) => name.ToLowerInvariant() switch
    {
        "trendyol" => Color.Primary,
        "hepsiburada" => Color.Secondary,
        "n11" => Color.Info,
        "amazon" => Color.Tertiary,
        _ => Color.Default
    };

    private record MarketplaceStatusViewModel(
        string Name, string Icon, Color Color,
        int SyncedCount, int PendingCount, int FailedCount);
}
