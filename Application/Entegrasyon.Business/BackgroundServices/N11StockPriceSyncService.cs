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
/// Her 15 dakikada N11'deki yayında ürünlerin stok ve fiyat bilgilerini senkronize eder.
/// N11 SOAP API ile ürün bazlı UpdatePrice + UpdateStock çağrıları yapar.
/// </summary>
public class N11StockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<N11StockPriceSyncService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int N11MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId;

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(15);

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var stockPriceService = services.GetRequiredService<IN11StockPriceService>();

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
            logger.LogDebug("N11 stock/price sync: yayında ürün yok, tenant {TenantId}", tenantId);
            return;
        }

        logger.LogInformation("N11 stock/price sync: {Count} ürün kontrol ediliyor, tenant {TenantId}",
            publishedProducts.Count, tenantId);

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
                    logger.LogWarning("N11 price sync başarısız: ProductId={ProductId}, {Message}, tenant {TenantId}",
                        product.Id, priceResult.Message, tenantId);
                if (!stockResult.Success)
                    logger.LogWarning("N11 stock sync başarısız: ProductId={ProductId}, {Message}, tenant {TenantId}",
                        product.Id, stockResult.Message, tenantId);
            }
        }

        logger.LogInformation("N11 stock/price sync tamamlandı, tenant {TenantId}: {Success} başarılı, {Error} başarısız",
            tenantId, successCount, errorCount);
    }
}
