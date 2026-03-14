using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// StockPriceChangedEvent'leri tüketir ve Trendyol'a stok/fiyat güncelleme gönderir.
/// Yalnızca Trendyol'da yayında olan ürünlerin varyantları için çalışır.
/// </summary>
public class TrendyolStockPriceSyncService(
    EventChannel<StockPriceChangedEvent> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<TrendyolStockPriceSyncService> logger) : BackgroundService
{
    private const int TrendyolMarketPlaceId = 1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
                var stockPriceService = scope.ServiceProvider.GetRequiredService<ITrendyolStockPriceService>();

                // Ürünün Trendyol'da yayında olup olmadığını kontrol et
                var pm = await dbContext.ProductMarketplaces.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ProductId == evt.ProductId
                        && x.MarketPlaceId == TrendyolMarketPlaceId
                        && x.Status == MarketplaceProductStatus.Published, stoppingToken);

                if (pm is null)
                {
                    logger.LogDebug("Product {ProductId} is not published on Trendyol, skipping stock/price sync",
                        evt.ProductId);
                    continue;
                }

                // Varyantı çek — stok ve fiyat bilgisi
                var variant = await dbContext.ProductVariants.AsNoTracking()
                    .Include(v => v.BranchOfficeStocks)
                    .FirstOrDefaultAsync(v => v.Id == evt.ProductVariantId, stoppingToken);

                if (variant is null) continue;

                // Marketplace'e stok gönderecek depo ID'leri
                var warehouseIds = await dbContext.MarketPlaceWarehouses.AsNoTracking()
                    .Where(w => w.MarketPlaceId == TrendyolMarketPlaceId)
                    .Select(w => w.BranchOfficeId)
                    .ToListAsync(stoppingToken);

                if (warehouseIds.Count == 0)
                {
                    warehouseIds = await dbContext.BranchOffices.AsNoTracking()
                        .Where(b => b.IsDefaultMarketPlaceStock)
                        .Select(b => b.Id)
                        .ToListAsync(stoppingToken);
                }

                var quantity = variant.BranchOfficeStocks
                    .Where(s => warehouseIds.Contains(s.BranchOfficeId))
                    .Sum(s => s.CurrentStock);

                var salePrice = variant.SalePrice > variant.ListPrice ? variant.ListPrice : variant.SalePrice;

                var items = new List<TrendyolPriceInventoryItem>
                {
                    new(variant.Barcode, quantity, salePrice, variant.ListPrice)
                };

                var result = await stockPriceService.UpdatePriceAndInventoryAsync(items);

                if (result.Success)
                    logger.LogInformation("Stock/price update sent to Trendyol for variant {Barcode}", variant.Barcode);
                else
                    logger.LogWarning("Stock/price update failed for variant {Barcode}: {Message}", variant.Barcode, result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync stock/price for variant {VariantId}", evt.ProductVariantId);
            }
        }
    }
}
