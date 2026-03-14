using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Mock stok/fiyat güncelleme servisi — request body'yi loglar, success döner.
/// </summary>
public sealed class MockTrendyolStockPriceService(
    ILogger<MockTrendyolStockPriceService> logger) : ITrendyolStockPriceService
{
    public Task<IDataResult<string>> UpdatePriceAndInventoryAsync(List<TrendyolPriceInventoryItem> items)
    {
        logger.LogInformation("Mock: Stock/price update for {Count} items:\n{Body}",
            items.Count,
            JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true }));

        var batchId = $"mock-stock-{Guid.NewGuid():N}";

        return Task.FromResult<IDataResult<string>>(
            new SuccessDataResult<string>(batchId, $"{items.Count} ürünün stok/fiyat bilgisi güncellendi (mock)."));
    }
}
