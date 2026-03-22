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
/// Amazon stok ve fiyat periyodik senkronizasyon servisi.
/// Published ürünlerin stok/fiyat bilgilerini Feeds API üzerinden gönderir.
/// </summary>
public class AmazonStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<AmazonStockPriceSyncService> logger) : BackgroundService
{
    private const int AmazonMpId = MarketPlaceConstants.AmazonMarketPlaceId;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Amazon stock/price sync cycle failed");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var feedService = scope.ServiceProvider.GetRequiredService<IAmazonFeedService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == AmazonMpId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Include(pm => pm.Product)
                .ThenInclude(p => p.ProductVariants)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("Amazon stock/price sync: yayında ürün yok");
            return;
        }

        // TODO: JSON_LISTINGS_FEED formatında stok/fiyat feed oluştur ve gönder
        // var feedContent = BuildStockPriceFeed(publishedProducts);
        // await feedService.SubmitFeedAsync("JSON_LISTINGS_FEED", "application/json", feedContent, marketplaceIds, ct);

        logger.LogInformation("Amazon stock/price sync: {Count} ürün kontrol edildi", publishedProducts.Count);
    }
}
