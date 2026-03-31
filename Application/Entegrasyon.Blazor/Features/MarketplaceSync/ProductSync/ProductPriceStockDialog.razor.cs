using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Interfaces;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class ProductPriceStockDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public Guid ProductId { get; set; }
    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public MarketplaceOverrideDetailDto CurrentOverrides { get; set; } = new();

    [Inject] private IMarketplaceOverrideManager OverrideManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    // Mutable clones for editing
    private List<VariantPriceOverrideDetailDto> _variantOverrides = [];
    private bool _syncImmediately;
    private bool _isBusy;

    protected override void OnInitialized()
    {
        // Deep clone — edit independently without touching CurrentOverrides
        _variantOverrides = CurrentOverrides.VariantOverrides.Select(v => new VariantPriceOverrideDetailDto
        {
            ProductVariantId = v.ProductVariantId,
            VariantLabel = v.VariantLabel,
            Barcode = v.Barcode,
            OriginalListPrice = v.OriginalListPrice,
            OriginalSalePrice = v.OriginalSalePrice,
            ListPriceOverride = v.ListPriceOverride,
            SalePriceOverride = v.SalePriceOverride
        }).ToList();
    }

    private async Task Submit()
    {
        _isBusy = true;
        try
        {
            var dto = new SaveMarketplaceOverridesDto
            {
                ProductId = ProductId,
                MarketPlaceId = MarketPlaceId,
                TitleOverride = CurrentOverrides.TitleOverride,
                DescriptionOverride = CurrentOverrides.DescriptionOverride,
                VariantOverrides = _variantOverrides.Select(v => new VariantPriceOverrideDto
                {
                    ProductVariantId = v.ProductVariantId,
                    ListPriceOverride = v.ListPriceOverride,
                    SalePriceOverride = v.SalePriceOverride
                }).ToList()
            };

            var result = _syncImmediately
                ? await OverrideManager.SaveOverridesAndPublishAsync(dto)
                : await OverrideManager.SaveOverridesAsync(dto);

            if (result.Success)
            {
                Snackbar.Add("Fiyat/Stok güncellendi", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message ?? "Hata oluştu", Severity.Error);
            }
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
