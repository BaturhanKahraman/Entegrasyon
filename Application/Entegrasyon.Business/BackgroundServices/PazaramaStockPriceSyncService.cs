using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 15 dakikada Pazarama'daki yayında ürünlerin stok ve fiyat bilgilerini senkronize eder.
/// Pazarama REST API ile toplu stok/fiyat güncelleme yapar.
/// </summary>
public class PazaramaStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<PazaramaStockPriceSyncService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int PazaramaMarketPlaceId = MarketPlaceConstants.PazaramaMarketPlaceId;

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(15);

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var stockPriceService = services.GetRequiredService<IPazaramaStockPriceService>();

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
            logger.LogDebug("Pazarama stock/price sync: yayında ürün yok, tenant {TenantId}", tenantId);
            return;
        }

        logger.LogInformation("Pazarama stock/price sync: {Count} ürün kontrol ediliyor, tenant {TenantId}",
            publishedProducts.Count, tenantId);

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
            logger.LogDebug("Pazarama stock/price sync: güncellenecek item yok, tenant {TenantId}", tenantId);
            return;
        }

        var stockResult = await stockPriceService.UpdateStockAsync(stockItems);
        if (stockResult.Success)
        {
            logger.LogInformation("Pazarama stock sync tamamlandı, tenant {TenantId}: {Count} item, dataId={DataId}",
                tenantId, stockItems.Count, stockResult.Data);
        }
        else
        {
            logger.LogWarning("Pazarama stock sync başarısız, tenant {TenantId}: {Message}",
                tenantId, stockResult.Message);
        }

        var priceResult = await stockPriceService.UpdatePriceAsync(priceItems);
        if (priceResult.Success)
        {
            logger.LogInformation("Pazarama price sync tamamlandı, tenant {TenantId}: {Count} item, dataId={DataId}",
                tenantId, priceItems.Count, priceResult.Data);
        }
        else
        {
            logger.LogWarning("Pazarama price sync başarısız, tenant {TenantId}: {Message}",
                tenantId, priceResult.Message);
        }
    }
}
