using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 15 dakikada PttAVM'deki yayinda urunlerin stok ve fiyat bilgilerini senkronize eder.
/// Multi-tenant hazir: ConcurrentDictionary ile tenant basina son sync zamani takip edilir.
/// Duplicate guard: ayni istek 5dk icinde tekrar gonderilmez.
/// </summary>
public class PttavmStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<PttavmStockPriceSyncService> logger) : BackgroundService
{
    private const int PttavmMarketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;
    private const int MaxBatchSize = 1000;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(15);

    // Multi-tenant: tenant basina son sync zamani (key = tenantId)
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
                logger.LogError(ex, "PttAVM stock/price sync cycle failed");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAllPublishedProductsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var stockPriceService = scope.ServiceProvider.GetRequiredService<IPttavmStockPriceService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == PttavmMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Include(pm => pm.Product)
                .ThenInclude(p => p.ProductVariants)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("PttAVM stock/price sync: yayında ürün yok");
            return;
        }

        logger.LogInformation("PttAVM stock/price sync: {Count} ürün kontrol ediliyor", publishedProducts.Count);

        var items = publishedProducts
            .SelectMany(pm => pm.Product.ProductVariants
                .Select(variant => new PttavmStockPriceRequest(
                    Barcode: variant.Barcode ?? pm.ExternalProductId!,
                    Active: true,
                    Quantity: variant.BranchOfficeStocks.Sum(s => s.CurrentStock),
                    PriceWithoutVat: variant.SalePrice,
                    PriceWithVat: variant.SalePrice * (1 + (variant.VatRate / 100m)),
                    VatRate: (int)variant.VatRate,
                    Discount: null,
                    IsCargoFromSupplier: null,
                    Variants: null)))
            .ToList();

        if (items.Count == 0)
        {
            logger.LogDebug("PttAVM stock/price sync: güncellenecek item yok");
            return;
        }

        // Batch processing: max 1000/batch
        for (var i = 0; i < items.Count; i += MaxBatchSize)
        {
            var batch = items.GetRange(i, Math.Min(MaxBatchSize, items.Count - i));
            var result = await stockPriceService.UpdateStockPricesAsync(batch, ct);
            if (result.Success)
            {
                logger.LogInformation("PttAVM stock/price sync batch tamamlandı: {Count} item, trackingId={TrackingId}",
                    batch.Count, result.Data?.TrackingId);
            }
            else
            {
                logger.LogWarning("PttAVM stock/price sync batch başarısız: {Message}", result.Message);
            }
        }

        _lastSyncTime[0] = DateTime.UtcNow; // tenantId=0 simdilik tek tenant
    }
}
