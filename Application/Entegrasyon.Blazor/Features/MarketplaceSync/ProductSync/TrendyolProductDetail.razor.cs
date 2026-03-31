using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class TrendyolProductDetail : ComponentBase
{
    [Parameter, EditorRequired] public Guid ProductId { get; set; }
    [Parameter, EditorRequired] public MarketplaceSyncItemDto SyncItem { get; set; } = null!;
    [Parameter] public MarketplaceOverrideDetailDto? Overrides { get; set; }
    [Parameter] public EventCallback OnSyncCompleted { get; set; }

    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private ITrendyolProductService TrendyolService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private bool _isBusy;

    private bool IsProcessing => SyncItem.SyncState == MarketplaceSyncState.Processing;

    private Color GetApprovalColor() =>
        SyncItem.IsApproved == true && SyncItem.IsArchived != true ? Color.Success :
        SyncItem.IsApproved == false ? Color.Error :
        SyncItem.IsArchived == true ? Color.Warning : Color.Default;

    private string GetApprovalLabel() =>
        (SyncItem.IsApproved, SyncItem.IsArchived) switch
        {
            (true, true) => "Arşivlendi",
            (true, _) => "Onaylı",
            (false, _) => "Reddedildi",
            _ => "Beklemede"
        };

    private async Task OpenContentUpdateDialog()
    {
        var parameters = new DialogParameters<ProductContentUpdateDialog>
        {
            { x => x.ProductId, ProductId },
            { x => x.MarketPlaceId, 1 },
            { x => x.CurrentOverrides, Overrides ?? new MarketplaceOverrideDetailDto() }
        };

        var dialog = await DialogService.ShowAsync<ProductContentUpdateDialog>(
            "İçerik Güncelle",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });

        var result = await dialog.Result;
        if (result is { Canceled: false })
            await OnSyncCompleted.InvokeAsync();
    }

    private async Task OpenPriceStockDialog()
    {
        var parameters = new DialogParameters<ProductPriceStockDialog>
        {
            { x => x.ProductId, ProductId },
            { x => x.MarketPlaceId, 1 },
            { x => x.CurrentOverrides, Overrides ?? new MarketplaceOverrideDetailDto() }
        };

        var dialog = await DialogService.ShowAsync<ProductPriceStockDialog>(
            "Fiyat / Stok Güncelle",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true });

        var result = await dialog.Result;
        if (result is { Canceled: false })
            await OnSyncCompleted.InvokeAsync();
    }

    private async Task HandleSync()
    {
        _isBusy = true;
        try
        {
            var result = await SyncManager.SyncProductAsync(ProductId, marketPlaceId: 1);
            Snackbar.Add(result.Message ?? "", result.Success ? Severity.Success : Severity.Error);
            if (result.Success)
                await OnSyncCompleted.InvokeAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task HandleDelete()
    {
        var confirm = await DialogService.ShowMessageBox(
            "Trendyol'dan Sil",
            "Bu ürünü Trendyol'dan silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            yesText: "Sil",
            cancelText: "İptal");

        if (confirm != true) return;

        _isBusy = true;
        try
        {
            var result = await TrendyolService.DeleteProductAsync(ProductId);
            Snackbar.Add(result.Message ?? "", result.Success ? Severity.Success : Severity.Error);
            if (result.Success)
                await OnSyncCompleted.InvokeAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task CopyToClipboard(string text)
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        Snackbar.Add("Kopyalandı", Severity.Info, cfg => cfg.ShowCloseIcon = false);
    }
}
