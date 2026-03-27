using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Hepsiburada stok ve fiyat periyodik senkronizasyon servisi.
/// Published durumundaki ürünlerin stok/fiyat bilgilerini Hepsiburada'ya gönderir.
/// </summary>
public class HepsiburadaStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<HepsiburadaStockPriceSyncService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(15);

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var listingService = services.GetRequiredService<IHepsiburadaListingService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == HbMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Select(pm => pm.ProductId)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("HB stock/price sync: no published products to sync for tenant {TenantId}", tenantId);
            return;
        }

        logger.LogInformation("HB stock/price sync: syncing {Count} products for tenant {TenantId}",
            publishedProducts.Count, tenantId);

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
                logger.LogWarning(ex, "HB sync failed for product {ProductId}, tenant {TenantId}",
                    productId, tenantId);
            }
        }

        logger.LogInformation("HB stock/price sync completed for tenant {TenantId}: {Success} success, {Fail} fail",
            tenantId, successCount, failCount);
    }
}
