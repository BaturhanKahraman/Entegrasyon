using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 15 dakikada Pazarama'daki yayında ürünlerin stok ve fiyat bilgilerini senkronize eder.
/// Multi-tenant hazır: ConcurrentDictionary ile tenant başına son sync zamanı takip edilir.
/// Pazarama REST API ile toplu stok/fiyat güncelleme yapar.
/// </summary>
public class PazaramaStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<PazaramaStockPriceSyncService> logger) : BackgroundService
{
    private const int PazaramaMarketPlaceId = MarketPlaceConstants.PazaramaMarketPlaceId;
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
                logger.LogError(ex, "Pazarama stock/price sync cycle failed");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAllPublishedProductsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var stockPriceService = scope.ServiceProvider.GetRequiredService<IPazaramaStockPriceService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == PazaramaMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Include(pm => pm.Product)
                .ThenInclude(p => p.ProductVariants)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("Pazarama stock/price sync: yayında ürün yok");
            return;
        }

        logger.LogInformation("Pazarama stock/price sync: {Count} ürün kontrol ediliyor", publishedProducts.Count);

        // Pazarama toplu güncelleme destekler — tüm item'ları toplayıp tek istekte gönder
        var stockItems = publishedProducts
            .SelectMany(pm => pm.Product.ProductVariants
                .Select(variant => new PazaramaStockUpdateItem(
                    Code: variant.Barcode ?? pm.ExternalProductId!,
                    StockCount: variant.BranchOfficeStocks.Sum(s => s.CurrentStock))))
            .ToList();

        var priceItems = publishedProducts
            .SelectMany(pm => pm.Product.ProductVariants
                .Select(variant => new PazaramaPriceUpdateItem(
                    Code: variant.Barcode ?? pm.ExternalProductId!,
                    ListPrice: variant.ListPrice,
                    SalePrice: variant.SalePrice)))
            .ToList();

        if (stockItems.Count == 0)
        {
            logger.LogDebug("Pazarama stock/price sync: güncellenecek item yok");
            return;
        }

        var stockResult = await stockPriceService.UpdateStockAsync(stockItems);
        if (stockResult.Success)
        {
            logger.LogInformation("Pazarama stock sync tamamlandı: {Count} item, dataId={DataId}",
                stockItems.Count, stockResult.Data);
        }
        else
        {
            logger.LogWarning("Pazarama stock sync başarısız: {Message}", stockResult.Message);
        }

        var priceResult = await stockPriceService.UpdatePriceAsync(priceItems);
        if (priceResult.Success)
        {
            logger.LogInformation("Pazarama price sync tamamlandı: {Count} item, dataId={DataId}",
                priceItems.Count, priceResult.Data);
        }
        else
        {
            logger.LogWarning("Pazarama price sync başarısız: {Message}", priceResult.Message);
        }

        _lastSyncTime[0] = DateTime.UtcNow; // tenantId=0 şimdilik tek tenant
    }
}
