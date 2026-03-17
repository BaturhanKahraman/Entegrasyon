using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Gercek Trendyol stok/fiyat guncelleme servisi.
/// POST /integration/inventory/sellers/{sellerId}/products/price-and-inventory
/// Bu endpoint UNLIMITED rate'e sahip.
/// </summary>
public sealed class TrendyolStockPriceService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ITrendyolApiClient apiClient,
    ILogger<TrendyolStockPriceService> logger) : ITrendyolStockPriceService
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IDataResult<string>> UpdatePriceAndInventoryAsync(List<TrendyolPriceInventoryItem> items)
    {
        if (items.Count == 0)
            return new ErrorDataResult<string>(null!, "Guncellenecek urun yok.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorDataResult<string>(null!, "Trendyol SellerId ayarlanmamis.");

        var url = $"integration/inventory/sellers/{marketplace.SellerId}/products/price-and-inventory";
        var request = new TrendyolPriceAndInventoryRequest(items);
        var response = await apiClient.PostAsync(url, request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol stock/price update failed. Status={Status}, Body={Body}",
                response.StatusCode, errorBody);
            return new ErrorDataResult<string>(null!, $"Trendyol API hatasi: {response.StatusCode}");
        }

        var batchResponse = await response.Content.ReadFromJsonAsync<TrendyolBatchResponse>();
        logger.LogInformation("Stock/price update sent to Trendyol. Items={Count}, BatchId={BatchId}",
            items.Count, batchResponse?.BatchRequestId);

        return new SuccessDataResult<string>(batchResponse?.BatchRequestId ?? "ok",
            $"{items.Count} urunun stok/fiyat bilgisi guncellendi.");
    }
}
