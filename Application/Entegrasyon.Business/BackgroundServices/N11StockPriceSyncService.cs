using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 15 dakikada N11'deki yayında ürünlerin stok ve fiyat bilgilerini senkronize eder.
/// Multi-tenant hazır: ConcurrentDictionary ile tenant başına son sync zamanı takip edilir.
/// N11 SOAP API ile ürün bazlı UpdatePrice + UpdateStock çağrıları yapar.
/// </summary>
public class N11StockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<N11StockPriceSyncService> logger) : BackgroundService
{
    private const int N11MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(15);

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
                logger.LogError(ex, "N11 stock/price sync cycle failed");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAllPublishedProductsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var stockPriceService = scope.ServiceProvider.GetRequiredService<IN11StockPriceService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == N11MarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Include(pm => pm.Product)
                .ThenInclude(p => p.ProductVariants)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("N11 stock/price sync: yayında ürün yok");
            return;
        }

        logger.LogInformation("N11 stock/price sync: {Count} ürün kontrol ediliyor", publishedProducts.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var pm in publishedProducts)
        {
            var product = pm.Product;
            var totalStock = product.ProductVariants
                .SelectMany(v => v.BranchOfficeStocks)
                .Sum(s => s.CurrentStock);

            var salePrice = product.ProductVariants
                .Select(v => v.SalePrice)
                .FirstOrDefault();

            // N11 SOAP API ürün bazlı çalışır — her ürün için ayrı çağrı
            var priceResult = await stockPriceService.UpdatePriceAsync(product.Id, salePrice);
            var stockResult = await stockPriceService.UpdateStockAsync(product.Id, totalStock);

            if (priceResult.Success && stockResult.Success)
            {
                successCount++;
            }
            else
            {
                errorCount++;
                if (!priceResult.Success)
                    logger.LogWarning("N11 price sync başarısız: ProductId={ProductId}, {Message}",
                        product.Id, priceResult.Message);
                if (!stockResult.Success)
                    logger.LogWarning("N11 stock sync başarısız: ProductId={ProductId}, {Message}",
                        product.Id, stockResult.Message);
            }
        }

        logger.LogInformation("N11 stock/price sync tamamlandı: {Success} başarılı, {Error} başarısız",
            successCount, errorCount);

        _lastSyncTime[0] = DateTime.UtcNow; // tenantId=0 şimdilik tek tenant
    }
}
