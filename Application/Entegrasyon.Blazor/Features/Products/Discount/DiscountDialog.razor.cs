using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product.Discount;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Products.Discount;

public partial class DiscountDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired] public DiscountPreviewDto Preview { get; set; } = null!;

    [Inject] private IDiscountManager DiscountManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private decimal _discountPercentage = 10;
    private bool _applying;
    private List<MarketplaceSelection> _availableMarketplaces = [];

    protected override void OnParametersSet()
    {
        _availableMarketplaces = Preview.MarketplacePrices
            .Select(m => new MarketplaceSelection(m.MarketPlaceId, m.MarketPlaceName))
            .DistinctBy(m => m.Id)
            .ToList();
    }

    private decimal CalculateNewPrice(decimal currentSalePrice)
        => Math.Round(currentSalePrice * (1 - _discountPercentage / 100), 2);

    private void Cancel() => MudDialog.Cancel();

    private async Task Apply()
    {
        _applying = true;
        try
        {
            var dto = new ApplyDiscountDto(
                Preview.ProductId,
                _discountPercentage,
                _availableMarketplaces.Where(m => m.Selected).Select(m => m.Id).ToList());

            var result = await DiscountManager.ApplyDiscountAsync(dto);

            if (result.Success)
            {
                var msg = $"{result.Data.VariantsUpdated} varyanta %{_discountPercentage} indirim uygulandı.";
                if (result.Data.SyncTriggeredMarketplaces.Count > 0)
                    msg += $" Senkronizasyon: {string.Join(", ", result.Data.SyncTriggeredMarketplaces)}";

                Snackbar.Add(msg, Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message ?? "", Severity.Error);
            }
        }
        catch (FluentValidation.ValidationException ex)
        {
            Snackbar.Add(string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)), Severity.Warning);
        }
        finally
        {
            _applying = false;
        }
    }

    private sealed class MarketplaceSelection(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public bool Selected { get; set; } = true;
    }
}
