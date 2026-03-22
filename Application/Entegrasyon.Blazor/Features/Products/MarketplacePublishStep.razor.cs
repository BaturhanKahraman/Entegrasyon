using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Blazor.Features.Products;

public partial class MarketplacePublishStep
{
    [Parameter] public Guid ProductId { get; set; }
    [Parameter] public EventCallback OnPublished { get; set; }
    [Parameter] public EventCallback OnSkipped { get; set; }

    [Inject] private IMarketplaceOverrideManager? OverrideManager { get; set; }
    [Inject] private ISnackbar? Snackbar { get; set; }

    private bool _loading = true;
    private bool _publishing;
    private bool _trendyolSelected;
    private string? _trendyolTitleOverride;
    private string? _trendyolDescriptionOverride;
    private List<VariantPriceOverrideDetailDto> _trendyolVariants = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadOverridesAsync();
    }

    private async Task LoadOverridesAsync()
    {
        _loading = true;
        try
        {
            if (OverrideManager is null) return;

            var result = await OverrideManager.GetOverridesAsync(ProductId, TrendyolMarketPlaceId);
            if (result.Success && result.Data is not null)
            {
                _trendyolTitleOverride = result.Data.TitleOverride;
                _trendyolDescriptionOverride = result.Data.DescriptionOverride;
                _trendyolVariants = result.Data.VariantOverrides;
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task PublishToTrendyol()
    {
        if (OverrideManager is null) return;

        _publishing = true;
        try
        {
            var dto = new SaveMarketplaceOverridesDto
            {
                ProductId = ProductId,
                MarketPlaceId = TrendyolMarketPlaceId,
                TitleOverride = string.IsNullOrWhiteSpace(_trendyolTitleOverride) ? null : _trendyolTitleOverride,
                DescriptionOverride = string.IsNullOrWhiteSpace(_trendyolDescriptionOverride) ? null : _trendyolDescriptionOverride,
                VariantOverrides = _trendyolVariants
                    .Select(v => new VariantPriceOverrideDto
                    {
                        ProductVariantId = v.ProductVariantId,
                        ListPriceOverride = v.ListPriceOverride,
                        SalePriceOverride = v.SalePriceOverride
                    })
                    .ToList()
            };

            var result = await OverrideManager.SaveOverridesAndPublishAsync(dto);
            if (result.Success)
            {
                await OnPublished.InvokeAsync();
            }
            else
            {
                Snackbar?.Add(result.Message ?? "Gönderim başarısız.", Severity.Error);
            }
        }
        catch (FluentValidation.ValidationException vex)
        {
            Snackbar?.Add(string.Join(" | ", vex.Errors.Select(e => e.ErrorMessage)), Severity.Warning);
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Beklenmeyen hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _publishing = false;
        }
    }

    private async Task Skip()
    {
        await OnSkipped.InvokeAsync();
    }
}
