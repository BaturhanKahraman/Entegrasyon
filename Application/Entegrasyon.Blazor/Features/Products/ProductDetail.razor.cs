using Entegrasyon.Blazor.Features.Printing;
using Entegrasyon.Blazor.Features.Products.Discount;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductDetail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProductService ProductManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private ILabelService LabelService { get; set; } = null!;
    [Inject] private IDiscountManager DiscountManager { get; set; } = null!;

    private ProductDetailDto? _product;
    private MarketplaceSyncStatusDto? _syncStatus;
    private bool _loading = true;
    private List<OutOfSyncMarketplaceInfo> _outOfSyncMarketplaces = [];

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        var result = await ProductManager.GetProductDetailById(Id);
        if (!result.Success || result.Data is null)
        {
            Snackbar.Add("Ürün bulunamadı.", Severity.Error);
            _loading = false;
            NavigationManager.NavigateTo("/products", replace: true);
            return;
        }

        _product = result.Data;

        var syncDetail = await SyncManager.GetProductSyncDetailAsync(Id);
        if (syncDetail.Success && syncDetail.Data is not null)
        {
            var trendyol = syncDetail.Data.Marketplaces.FirstOrDefault(m => m.MarketPlaceId == 1);
            _syncStatus = trendyol is not null
                ? new MarketplaceSyncStatusDto(trendyol.SyncState, trendyol.LastSyncedAt, trendyol.BatchRequestId, trendyol.StatusMessage)
                : null;

            _outOfSyncMarketplaces = syncDetail.Data.Marketplaces
                .Where(m => m.SyncState == MarketplaceSyncState.OutOfSync)
                .Select(m => new OutOfSyncMarketplaceInfo(m.MarketPlaceId, m.MarketPlaceName))
                .ToList();
        }

        _loading = false;
    }

    private void GoToEditGeneral() => NavigationManager.NavigateTo($"/products/edit/{Id}?tab=general");
    private void GoToEditStock() => NavigationManager.NavigateTo($"/products/edit/{Id}?tab=variants");
    private void GoToEditCategory() => NavigationManager.NavigateTo($"/products/edit/{Id}?tab=attributes");
    private void GoToCategory() => NavigationManager.NavigateTo($"/categories/edit/{_product!.CategoryId}");

    private void GoToBrand()
    {
        if (_product?.BrandId.HasValue == true)
            NavigationManager.NavigateTo("/brands");
    }

    private void GoBack() => NavigationManager.NavigateTo("/products");

    private bool CanSync => _syncStatus?.State is MarketplaceSyncState.OutOfSync
        or MarketplaceSyncState.Failed or MarketplaceSyncState.Rejected;

    private (Color Color, string Label) SyncChipInfo => _syncStatus?.State switch
    {
        MarketplaceSyncState.Synced => (Color.Success, "Yayında"),
        MarketplaceSyncState.OutOfSync => (Color.Warning, "Güncelleme Gerekiyor"),
        MarketplaceSyncState.Processing => (Color.Info, "İşleniyor"),
        MarketplaceSyncState.Waiting => (Color.Warning, "Bekliyor"),
        MarketplaceSyncState.Failed => (Color.Error, "Başarısız"),
        MarketplaceSyncState.Rejected => (Color.Error, "Reddedildi"),
        _ => (Color.Default, "")
    };

    private async Task SyncToMarketplace()
    {
        if (_syncStatus?.State is MarketplaceSyncState.Processing)
        {
            Snackbar.Add("Ürün şu an marketplace'te işleniyor. Batch tamamlandıktan sonra tekrar gönderebilirsiniz.", Severity.Warning);
            return;
        }

        var result = await SyncManager.SyncProductAsync(Id, marketPlaceId: 1);
        Snackbar.Add(
            result.Success ? "Ürün marketplace gönderim kuyruğuna eklendi." : result.Message,
            result.Success ? Severity.Success : Severity.Error);

        if (result.Success)
            _syncStatus = new MarketplaceSyncStatusDto(MarketplaceSyncState.Waiting, _syncStatus?.LastSyncedAt, null, null);
    }

    private async Task PrintBarcode()
    {
        if (_product is null) return;

        var firstVariant = _product.ProductVariantsDetails.FirstOrDefault();
        if (firstVariant is null)
        {
            Snackbar.Add("Ürünün varyantı bulunamadı", Severity.Warning);
            return;
        }

        var result = await LabelService.GenerateProductLabel(firstVariant.Id);
        if (!result.Success || result.Data is null)
        {
            Snackbar.Add(result.Message ?? "Etiket üretilemedi", Severity.Error);
            return;
        }

        var parameters = new DialogParameters<PrintDialog>
        {
            { x => x.PrintJob, result.Data }
        };

        await DialogService.ShowAsync<PrintDialog>("Barkod Yazdır", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
    }

    private async Task OpenDiscountDialog()
    {
        if (_product is null) return;

        var previewResult = await DiscountManager.GetDiscountPreviewAsync(Id);
        if (!previewResult.Success || previewResult.Data is null)
        {
            Snackbar.Add(previewResult.Message ?? "İndirim bilgileri yüklenemedi.", Severity.Error);
            return;
        }

        var parameters = new DialogParameters<DiscountDialog>
        {
            { x => x.Preview, previewResult.Data }
        };

        var dialog = await DialogService.ShowAsync<DiscountDialog>(
            "İndirim Uygula", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true });

        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            // Sayfayı yenile
            _loading = true;
            StateHasChanged();
            await OnInitializedAsync();
            StateHasChanged();
        }
    }

    private async Task SyncAllOutOfSync()
    {
        foreach (var mp in _outOfSyncMarketplaces)
        {
            var result = await SyncManager.SyncProductAsync(Id, mp.MarketPlaceId);
            Snackbar.Add(
                result.Success
                    ? $"{mp.Name} senkronizasyon kuyruğuna eklendi."
                    : $"{mp.Name}: {result.Message}",
                result.Success ? Severity.Success : Severity.Error);
        }

        _outOfSyncMarketplaces = [];
    }

    private async Task DeleteProduct()
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Ürünü Sil",
            "Bu ürünü silmek istediğinize emin misiniz?",
            yesText: "Sil",
            cancelText: "İptal");

        if (confirmed != true) return;

        var result = await ProductManager.SoftDeleteProduct(Id);
        Snackbar.Add(
            result.Success ? "Ürün silindi." : result.Message,
            result.Success ? Severity.Success : Severity.Error);

        if (result.Success)
            NavigationManager.NavigateTo("/products", replace: true);
    }
}
