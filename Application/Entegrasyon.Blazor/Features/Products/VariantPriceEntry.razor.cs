using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Constants;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Products;

public partial class VariantPriceEntry
{
    [Parameter, EditorRequired]
    public AddProductVariantDto Variant { get; set; } = null!;

    [Parameter]
    public decimal? DefaultVatRate { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    private bool _isKdvInclusive;
    private bool _isCustomRate;
    private decimal? _selectedPresetRate;

    private decimal _effectiveVatRate => Variant.VatRate ?? 0;

    protected override void OnParametersSet()
    {
        if (!Variant.VatRate.HasValue && DefaultVatRate.HasValue)
        {
            Variant.VatRate = DefaultVatRate;
        }

        // Determine if current rate is a preset or custom
        if (Variant.VatRate.HasValue && KdvRates.Presets.Contains(Variant.VatRate.Value))
        {
            _selectedPresetRate = Variant.VatRate;
            _isCustomRate = false;
        }
        else if (Variant.VatRate.HasValue && Variant.VatRate.Value > 0)
        {
            _selectedPresetRate = -1; // "Ozel" selected
            _isCustomRate = true;
        }
        else if (!Variant.VatRate.HasValue || Variant.VatRate == 0)
        {
            // Default to %20 preset
            _selectedPresetRate = KdvRates.Default;
            Variant.VatRate = KdvRates.Default;
            _isCustomRate = false;
        }
    }

    private async Task OnPresetRateChanged(decimal? value)
    {
        _selectedPresetRate = value;
        if (value == -1)
        {
            _isCustomRate = true;
            // Keep current VatRate or set to null
            if (!Variant.VatRate.HasValue || KdvRates.Presets.Contains(Variant.VatRate.Value))
                Variant.VatRate = null;
        }
        else
        {
            _isCustomRate = false;
            Variant.VatRate = value;
        }
        await OnChanged.InvokeAsync();
    }

    private async Task OnCustomRateChanged(decimal? value)
    {
        Variant.VatRate = value;
        await OnChanged.InvokeAsync();
    }

    private async Task OnListPriceChanged(decimal? value)
    {
        Variant.ListPrice = value;
        await OnChanged.InvokeAsync();
    }

    private async Task OnSalePriceChanged(decimal? value)
    {
        Variant.SalePrice = value;
        await OnChanged.InvokeAsync();
    }

    private async Task OnCostPriceChanged(decimal? value)
    {
        Variant.CostPrice = value;
        await OnChanged.InvokeAsync();
    }

    private decimal CalculateNetFromInclusive(decimal grossPrice)
        => KdvCalculator.FromInclusive(grossPrice, _effectiveVatRate).NetPrice;

    private decimal CalculateGrossFromExclusive(decimal netPrice)
        => KdvCalculator.FromExclusive(netPrice, _effectiveVatRate).GrossPrice;

    // Kar hesaplamaları
    private bool HasProfitData => Variant.SalePrice is > 0 && Variant.CostPrice is > 0;

    private decimal GrossProfit => (Variant.SalePrice ?? 0) - (Variant.CostPrice ?? 0);

    private decimal NetProfit => _effectiveVatRate > 0
        ? CalculateNetFromInclusive(Variant.SalePrice ?? 0) - CalculateNetFromInclusive(Variant.CostPrice ?? 0)
        : GrossProfit;

    private decimal ProfitMargin => Variant.SalePrice > 0
        ? GrossProfit / Variant.SalePrice.Value * 100
        : 0;
}
