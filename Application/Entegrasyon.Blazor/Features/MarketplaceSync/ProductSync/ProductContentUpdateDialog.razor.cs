using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Interfaces;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class ProductContentUpdateDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public Guid ProductId { get; set; }
    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public MarketplaceOverrideDetailDto CurrentOverrides { get; set; } = new();

    [Inject] private IMarketplaceOverrideManager OverrideManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private string? _titleOverride;
    private string? _descriptionOverride;
    private bool _syncImmediately;
    private bool _isBusy;
    private string? _validationError;

    protected override void OnInitialized()
    {
        _titleOverride = CurrentOverrides.TitleOverride;
        _descriptionOverride = CurrentOverrides.DescriptionOverride;
    }

    private bool Validate()
    {
        if (!string.IsNullOrWhiteSpace(_titleOverride) && (_titleOverride.Length < 3 || _titleOverride.Length > 200))
        {
            _validationError = "Başlık düzenlemesi 3-200 karakter arasında olmalıdır.";
            return false;
        }
        if (!string.IsNullOrWhiteSpace(_descriptionOverride) && (_descriptionOverride.Length < 20 || _descriptionOverride.Length > 2000))
        {
            _validationError = "Açıklama düzenlemesi 20-2000 karakter arasında olmalıdır.";
            return false;
        }
        _validationError = null;
        return true;
    }

    private async Task Submit()
    {
        if (!Validate()) return;

        _isBusy = true;
        try
        {
            var dto = new SaveMarketplaceOverridesDto
            {
                ProductId = ProductId,
                MarketPlaceId = MarketPlaceId,
                TitleOverride = string.IsNullOrWhiteSpace(_titleOverride) ? null : _titleOverride.Trim(),
                DescriptionOverride = string.IsNullOrWhiteSpace(_descriptionOverride) ? null : _descriptionOverride.Trim(),
                VariantOverrides = CurrentOverrides.VariantOverrides.Select(v => new VariantPriceOverrideDto
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
                Snackbar.Add("İçerik güncellendi", Severity.Success);
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
