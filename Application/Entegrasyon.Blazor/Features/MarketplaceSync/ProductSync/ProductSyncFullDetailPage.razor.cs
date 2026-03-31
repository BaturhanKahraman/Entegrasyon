using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class ProductSyncFullDetailPage : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private IMarketplaceOverrideManager OverrideManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private ProductSyncDetailDto? _detail;
    private MarketplaceOverrideDetailDto? _trendyolOverrides;
    private bool _loading = true;
    private int _activeTab;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;

        // Sequential loads — no Task.WhenAll (shared DbContext risk)
        var detailResult = await SyncManager.GetProductSyncDetailAsync(Id);
        if (!detailResult.Success || detailResult.Data is null)
        {
            Snackbar.Add("Ürün bulunamadı.", Severity.Error);
            _loading = false;
            return;
        }
        _detail = detailResult.Data;

        // Load Trendyol overrides if product has a Trendyol record
        var hasTrendyol = _detail.Marketplaces.Any(m => m.MarketPlaceId == 1 && m.SyncState != MarketplaceSyncState.NeverSynced);
        if (hasTrendyol)
        {
            var overridesResult = await OverrideManager.GetOverridesAsync(Id, 1);
            if (overridesResult.Success)
                _trendyolOverrides = overridesResult.Data;
        }

        _loading = false;
    }

    private async Task RefreshData()
    {
        await LoadData();
        StateHasChanged();
    }

    private void SetActiveTab(int marketPlaceId)
    {
        if (_detail is null) return;

        var tabMarketplaces = _detail.Marketplaces
            .Where(m => m.SyncState != MarketplaceSyncState.NeverSynced)
            .ToList();

        var idx = tabMarketplaces.FindIndex(m => m.MarketPlaceId == marketPlaceId);
        if (idx >= 0)
            _activeTab = idx;
    }

    private void GoBack() => NavigationManager.NavigateTo($"/products/{Id}");
}
