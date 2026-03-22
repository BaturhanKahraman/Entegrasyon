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
/// Hepsiburada stok ve fiyat periyodik senkronizasyon servisi.
/// Published durumundaki ürünlerin stok/fiyat bilgilerini Hepsiburada'ya gönderir.
/// </summary>
public class HepsiburadaStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<HepsiburadaStockPriceSyncService> logger) : BackgroundService
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uygulama başlangıcında bekle
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllPublishedProductsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "HB stock/price sync cycle failed");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAllPublishedProductsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var listingService = scope.ServiceProvider.GetRequiredService<IHepsiburadaListingService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == HbMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Select(pm => pm.ProductId)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("HB stock/price sync: no published products to sync");
            return;
        }

        logger.LogInformation("HB stock/price sync: syncing {Count} products", publishedProducts.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var productId in publishedProducts)
        {
            try
            {
                var result = await listingService.SyncProductStockAndPriceAsync(productId);
                if (result.Success)
                    successCount++;
                else
                    failCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                logger.LogWarning(ex, "HB sync failed for product {ProductId}", productId);
            }
        }

        logger.LogInformation("HB stock/price sync completed: {Success} success, {Fail} fail",
            successCount, failCount);
    }
}
