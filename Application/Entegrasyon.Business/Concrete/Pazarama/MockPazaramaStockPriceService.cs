using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama stok ve fiyat güncelleme servisi — mock implementasyon (test/geliştirme ortamı).
/// Gerçek API çağrısı yapmaz; her çağrı için benzersiz bir dataId üretir.
/// </summary>
public sealed class MockPazaramaStockPriceService(
    ILogger<MockPazaramaStockPriceService> logger) : IPazaramaStockPriceService
{
    private int _counter;

    public Task<IDataResult<string>> UpdateStockAsync(List<PazaramaStockUpdateItem> items)
    {
        var dataId = $"mock-pazarama-stock-{Interlocked.Increment(ref _counter)}";
        logger.LogInformation("MockPazarama: UpdateStock {Count} items -> {DataId}", items.Count, dataId);
        return Task.FromResult<IDataResult<string>>(new SuccessDataResult<string>(dataId));
    }

    public Task<IDataResult<string>> UpdatePriceAsync(List<PazaramaPriceUpdateItem> items)
    {
        var dataId = $"mock-pazarama-price-{Interlocked.Increment(ref _counter)}";
        logger.LogInformation("MockPazarama: UpdatePrice {Count} items -> {DataId}", items.Count, dataId);
        return Task.FromResult<IDataResult<string>>(new SuccessDataResult<string>(dataId));
    }
}
