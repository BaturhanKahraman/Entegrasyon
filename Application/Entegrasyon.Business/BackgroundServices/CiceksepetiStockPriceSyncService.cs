using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 5 dakikada Çiçeksepeti'nde yayındaki ürünlerin stok ve fiyat bilgilerini senkronize eder.
/// </summary>
public class CiceksepetiStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<CiceksepetiStockPriceSyncService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int CiceksepetiMarketPlaceId = MarketPlaceConstants.CiceksepetiMarketPlaceId;

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(5);

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var stockPriceService = services.GetRequiredService<ICiceksepetiStockPriceService>();

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
            logger.LogDebug("Çiçeksepeti stock/price sync: yayında ürün yok, tenant {TenantId}", tenantId);
            return;
        }

        logger.LogInformation("Çiçeksepeti stock/price sync: {Count} ürün kontrol ediliyor, tenant {TenantId}",
            publishedProducts.Count, tenantId);

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
            logger.LogDebug("Çiçeksepeti stock/price sync: güncellenecek item yok, tenant {TenantId}", tenantId);
            return;
        }

        var result = await stockPriceService.UpdateStockAndPriceAsync(items, ct);
        if (result.Success)
        {
            logger.LogInformation("Çiçeksepeti stock/price sync tamamlandı, tenant {TenantId}: {Count} item güncellendi",
                tenantId, items.Count);
        }
        else
        {
            logger.LogWarning("Çiçeksepeti stock/price sync başarısız, tenant {TenantId}: {Message}",
                tenantId, result.Message);
        }
    }
}
