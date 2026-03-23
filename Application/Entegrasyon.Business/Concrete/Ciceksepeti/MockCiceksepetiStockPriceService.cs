using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti stok/fiyat servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; mock batch ID'ler döndürür.
/// </summary>
public sealed class MockCiceksepetiStockPriceService(
    ILogger<MockCiceksepetiStockPriceService> logger) : ICiceksepetiStockPriceService
{
    public Task<IDataResult<List<string>>> UpdateStockAndPriceAsync(List<CiceksepetiStockPriceItem> items, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti stock/price update: {Count} items", items.Count);
        var batchIds = items.Select(i => $"mock-sp-{i.StockCode}").ToList();
        return Task.FromResult<IDataResult<List<string>>>(
            new SuccessDataResult<List<string>>(batchIds, $"{items.Count} stok/fiyat güncellendi (MOCK)."));
    }
}
