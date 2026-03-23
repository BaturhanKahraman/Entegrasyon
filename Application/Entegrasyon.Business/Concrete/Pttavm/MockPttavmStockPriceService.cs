using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// Mock PttAVM stok/fiyat servisi — development ve test ortamlari icin.
/// </summary>
public sealed class MockPttavmStockPriceService(
    ILogger<MockPttavmStockPriceService> logger) : IPttavmStockPriceService
{
    public Task<IDataResult<PttavmUpsertResult>> UpdateStockPricesAsync(
        List<PttavmStockPriceRequest> items, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM UpdateStockPrices: {Count} items", items.Count);
        var result = new PttavmUpsertResult(items.Count, Guid.NewGuid().ToString(), true, null);
        return Task.FromResult<IDataResult<PttavmUpsertResult>>(new SuccessDataResult<PttavmUpsertResult>(result));
    }

    public Task<IDataResult<List<PttavmProductInfo>>> SearchProductsAsync(
        PttavmProductSearchFilter filter, CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM SearchProducts: page={Page}", filter.SearchPage);
        var result = new List<PttavmProductInfo>
        {
            new(1, "MOCK-BC001", "Mock Product 1", 100, 80, 96, 20, true, true, 0, 1, 2, null, null)
        };
        return Task.FromResult<IDataResult<List<PttavmProductInfo>>>(new SuccessDataResult<List<PttavmProductInfo>>(result));
    }
}
