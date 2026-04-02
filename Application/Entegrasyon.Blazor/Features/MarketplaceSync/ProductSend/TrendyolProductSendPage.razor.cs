using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MarketplaceVariantPriceOverride = Entegrasyon.Entity.Dtos.Product.Marketplace.VariantPriceOverrideDto;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSend;

public partial class TrendyolProductSendPage : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private ITrendyolProductService TrendyolService { get; set; } = null!;
    [Inject] private IMarketplaceOverrideManager OverrideManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private ProductSyncDetailDto? _detail;
    private ProductSendPreflightDto? _preflight;
    private TrendyolSendPreviewDto? _preview;
    private List<VariantPricingRowDto>? _pricingRows;

    private bool _loading = true;
    private bool _preflightPassed = false;
    private bool _previewLoading = false;
    private bool _sending = false;

    private (string? Title, string? Description) _currentOverrides;
    private List<MarketplaceVariantPriceOverride> _priceOverrides = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;

        // Ürün detayı yükle
        var detailResult = await SyncManager.GetProductSyncDetailAsync(Id);
        if (!detailResult.Success || detailResult.Data is null)
        {
            Snackbar.Add("Ürün bulunamadı.", Severity.Error);
            _loading = false;
            return;
        }
        _detail = detailResult.Data;

        // Preflight kontrolleri çalıştır
        var preflightResult = await SyncManager.GetSendPreflightAsync(Id, 1);
        if (preflightResult.Success && preflightResult.Data is not null)
        {
            _preflight = preflightResult.Data;
            if (_preflight.AllPassed)
            {
                _preflightPassed = true;
                await LoadPreview();
                await LoadPricingRows();
            }
        }

        _loading = false;
    }

    private async Task LoadPreview()
    {
        _previewLoading = true;
        var overrides = new MarketplaceOverrideDetailDto
        {
            MarketPlaceId = 1,
            MarketPlaceName = "Trendyol",
            TitleOverride = _currentOverrides.Title,
            DescriptionOverride = _currentOverrides.Description,
            VariantOverrides = []
        };

        var previewResult = await TrendyolService.GetSendPreviewAsync(Id, overrides);
        if (previewResult.Success && previewResult.Data is not null)
            _preview = previewResult.Data;

        _previewLoading = false;
    }

    private async Task LoadPricingRows()
    {
        // Varyantları yükle — SyncManager'dan proje bilgisini al
        // Not: ProductSyncDetailDto sadece VariantCount içerir, varyant detayları SendPricingPanel tarafından talep edilecek
        _pricingRows = [];
    }

    private async Task HandleSendAsync()
    {
        var confirmed = await ShowConfirmationDialog();
        if (!confirmed) return;

        _sending = true;
        try
        {
            // Override'ları kaydet
            if (_currentOverrides.Title is not null || _currentOverrides.Description is not null || _priceOverrides.Count > 0)
            {
                var saveDto = new SaveMarketplaceOverridesDto
                {
                    ProductId = Id,
                    MarketPlaceId = 1,
                    TitleOverride = _currentOverrides.Title,
                    DescriptionOverride = _currentOverrides.Description,
                    VariantOverrides = _priceOverrides
                };
                await OverrideManager.SaveOverridesAsync(saveDto);
            }

            // Ürünü sync kuyruğuna ekle
            var result = await SyncManager.SyncProductAsync(Id, 1);
            Snackbar.Add(result.Message ?? "Ürün gönderme başlatıldı.",
                         result.Success ? Severity.Success : Severity.Error);

            if (result.Success)
                NavigationManager.NavigateTo($"/products/{Id}/sync");
        }
        finally
        {
            _sending = false;
        }
    }

    private async Task<bool> ShowConfirmationDialog()
    {
        try
        {
            var confirmed = await JS.InvokeAsync<bool>("confirm",
                "Bu ürünü Trendyol'a göndermek istediğinize emin misiniz?");
            return confirmed;
        }
        catch
        {
            return false;
        }
    }

    private void GoBack() => NavigationManager.NavigateTo($"/products/{Id}/sync");
}
