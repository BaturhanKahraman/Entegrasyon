using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 5 dakikada Çiçeksepeti'nde yayındaki ürünlerin stok ve fiyat bilgilerini senkronize eder.
/// Multi-tenant hazır: ConcurrentDictionary ile tenant başına son sync zamanı takip edilir.
/// </summary>
public class CiceksepetiStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<CiceksepetiStockPriceSyncService> logger) : BackgroundService
{
    private const int CiceksepetiMarketPlaceId = MarketPlaceConstants.CiceksepetiMarketPlaceId;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(5);

    // Multi-tenant: tenant başına son sync zamanı (key = tenantId)
    private readonly ConcurrentDictionary<int, DateTime> _lastSyncTime = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllPublishedProductsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Çiçeksepeti stock/price sync cycle failed");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAllPublishedProductsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var stockPriceService = scope.ServiceProvider.GetRequiredService<ICiceksepetiStockPriceService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == CiceksepetiMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Include(pm => pm.Product)
                .ThenInclude(p => p.ProductVariants)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("Çiçeksepeti stock/price sync: yayında ürün yok");
            return;
        }

        logger.LogInformation("Çiçeksepeti stock/price sync: {Count} ürün kontrol ediliyor", publishedProducts.Count);

        // Her yayında ürün için stok/fiyat item'ı oluştur
        var items = publishedProducts
            .SelectMany(pm => pm.Product.ProductVariants
                .Select(variant => new CiceksepetiStockPriceItem(
                    StockCode: variant.Barcode ?? pm.ExternalProductId!,
                    StockQuantity: variant.BranchOfficeStocks.Sum(s => s.CurrentStock),
                    SalesPrice: variant.SalePrice,
                    ListPrice: variant.ListPrice)))
            .ToList();

        if (items.Count == 0)
        {
            logger.LogDebug("Çiçeksepeti stock/price sync: güncellenecek item yok");
            return;
        }

        var result = await stockPriceService.UpdateStockAndPriceAsync(items, ct);
        if (result.Success)
        {
            logger.LogInformation("Çiçeksepeti stock/price sync tamamlandı: {Count} item güncellendi", items.Count);
        }
        else
        {
            logger.LogWarning("Çiçeksepeti stock/price sync başarısız: {Message}", result.Message);
        }

        _lastSyncTime[0] = DateTime.UtcNow; // tenantId=0 şimdilik tek tenant
    }
}
