using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class ProductSyncPage
{
    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private const int TrendyolMarketPlaceId = 1;

    private ProductSyncSummaryDto? _summary;
    private List<ProductSyncListItemDto> _items = [];
    private int _totalCount;
    private bool _loading = true;
    private string _searchKey = string.Empty;
    private MarketplaceSyncState? _stateFilter;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;
        _summary = await SyncManager.GetSyncSummaryAsync(TrendyolMarketPlaceId);
        var result = await SyncManager.GetProductSyncListAsync(
            TrendyolMarketPlaceId, _stateFilter, _searchKey, 0, 200);
        if (result.Success && result.Data is not null)
        {
            _items = result.Data.Items.ToList();
            _totalCount = result.Data.TotalItemCount;
        }
        _loading = false;
    }

    private async Task OnSearchChanged(string value)
    {
        _searchKey = value;
        await LoadData();
    }

    private async Task OnFilterChanged(MarketplaceSyncState? value)
    {
        _stateFilter = value;
        await LoadData();
    }

    private void OnRowClick(DataGridRowClickEventArgs<ProductSyncListItemDto> args)
    {
        NavigationManager.NavigateTo($"/marketplace/matching/{args.Item.ProductId}");
    }

    private async Task SyncAllPending()
    {
        var confirm = await DialogService.ShowMessageBox(
            "Tümünü Senkronize Et",
            "Henüz senkronize edilmemiş tüm ürünler kuyruğa eklenecek. Devam etmek istiyor musunuz?",
            yesText: "Evet", cancelText: "İptal");
        if (confirm != true) return;

        try
        {
            var result = await SyncManager.SyncAllPendingAsync(TrendyolMarketPlaceId);
            Snackbar.Add(result.Message, result.Success ? Severity.Success : Severity.Error);
            await LoadData();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
    }

    private async Task RetryAllFailed()
    {
        var confirm = await DialogService.ShowMessageBox(
            "Hatalıları Tekrarla",
            "Tüm başarısız/reddedilmiş ürünler yeniden kuyruğa eklenecek. Devam etmek istiyor musunuz?",
            yesText: "Evet", cancelText: "İptal");
        if (confirm != true) return;

        try
        {
            var result = await SyncManager.RetryAllFailedAsync(TrendyolMarketPlaceId);
            Snackbar.Add(result.Message, result.Success ? Severity.Success : Severity.Error);
            await LoadData();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
    }

    internal static Color GetSyncStateColor(MarketplaceSyncState state) => state switch
    {
        MarketplaceSyncState.Synced => Color.Success,
        MarketplaceSyncState.OutOfSync => Color.Warning,
        MarketplaceSyncState.Waiting or MarketplaceSyncState.Processing => Color.Info,
        MarketplaceSyncState.Failed or MarketplaceSyncState.Rejected => Color.Error,
        MarketplaceSyncState.NeverSynced => Color.Default,
        _ => Color.Default
    };

    internal static string GetSyncStateLabel(MarketplaceSyncState state) => state switch
    {
        MarketplaceSyncState.NeverSynced => "Senkronize Edilmedi",
        MarketplaceSyncState.Waiting => "Bekliyor",
        MarketplaceSyncState.Processing => "İşleniyor",
        MarketplaceSyncState.OutOfSync => "Güncelleme Gerekiyor",
        MarketplaceSyncState.Synced => "Yayında",
        MarketplaceSyncState.Failed => "Başarısız",
        MarketplaceSyncState.Rejected => "Reddedildi",
        _ => ""
    };
}
