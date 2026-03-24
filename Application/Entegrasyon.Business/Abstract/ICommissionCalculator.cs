using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICommissionCalculator
{
    Task<IDataResult<CommissionCalculationResult>> CalculateAsync(
        int marketPlaceId, int? categoryId, decimal salePrice, decimal costPrice,
        CancellationToken ct = default);

    Task<IDataResult<List<MarketplaceCommissionRateDto>>> GetCommissionRatesAsync(
        int marketPlaceId, CancellationToken ct = default);

    Task<IResult> SaveCommissionRateAsync(SaveCommissionRateDto dto, CancellationToken ct = default);

    Task<IResult> DeleteCommissionRateAsync(int rateId, CancellationToken ct = default);
}
