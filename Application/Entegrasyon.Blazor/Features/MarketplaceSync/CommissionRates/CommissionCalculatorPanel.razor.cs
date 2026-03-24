using System.Globalization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.CommissionRates;

public partial class CommissionCalculatorPanel
{
    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public int? CategoryId { get; set; }

    [Inject] private ICommissionCalculator CommissionCalculator { get; set; } = null!;

    private decimal _salePrice;
    private decimal _costPrice;
    private CommissionCalculationResult? _result;
    private string? _errorMessage;

    private static readonly CultureInfo _trCulture = new("tr-TR");

    private async Task CalculateAsync()
    {
        if (_salePrice <= 0)
        {
            _result = null;
            _errorMessage = null;
            return;
        }

        var calcResult = await CommissionCalculator.CalculateAsync(
            MarketPlaceId, CategoryId, _salePrice, _costPrice);

        if (calcResult.Success)
        {
            _result = calcResult.Data;
            _errorMessage = null;
        }
        else
        {
            _result = null;
            _errorMessage = calcResult.Message;
        }
    }
}
