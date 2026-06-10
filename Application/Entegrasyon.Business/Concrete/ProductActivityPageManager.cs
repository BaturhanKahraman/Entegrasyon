using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Constants;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Ürün 360° aktivite sayfasının veri orkestratörü.
/// Feature-gating: pazaryeri durum kartları + aktivite timeline yalnızca e-ticaret/pazaryeri
/// paketi aktifse sorgulanır. Sipariş + stok hareketleri fiziksel mağaza için de geçerli → gate yok.
///
/// Loglama: developer-facing ILogger her çağrıda; user-facing IApplicationLogManager yalnızca
/// hata durumunda (read-path'te her fetch'i user log'a yazmak spam + read üzerinde DB write olur).
/// </summary>
public sealed class ProductActivityPageManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IProductActivityLogger activityLogger,
    IFeatureService featureService,
    IApplicationLogManager applicationLogManager,
    ILogger<ProductActivityPageManager> logger) : IProductActivityPageManager
{
    public Task<bool> IsEcommerceEnabledAsync() =>
        featureService.IsFeatureEnabledAsync(PermissionConstants.MarketplaceView);

    public async Task<IDataResult<List<ProductMarketplaceStatusDto>>> GetMarketplaceStatusesAsync(
        Guid productId, bool? ecommerceEnabled = null)
    {
        if (!(ecommerceEnabled ?? await IsEcommerceEnabledAsync()))
        {
            logger.LogDebug("E-ticaret kapalı; pazaryeri durum kartları atlandı. ProductId={ProductId}", productId);
            return new SuccessDataResult<List<ProductMarketplaceStatusDto>>([]);
        }

        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            var statuses = await dbContext.ProductMarketplaces
                .AsNoTracking()
                .Where(pm => pm.ProductId == productId)
                .OrderBy(pm => pm.MarketPlaceId)
                .Select(pm => new ProductMarketplaceStatusDto(
                    pm.MarketPlaceId,
                    pm.MarketPlace.Name,
                    pm.Status,
                    pm.StatusMessage,
                    pm.ExternalProductId,
                    pm.IsApproved,
                    pm.LastSyncedAt))
                .ToListAsync();

            logger.LogInformation("Pazaryeri durum kartları yüklendi. ProductId={ProductId}, Count={Count}",
                productId, statuses.Count);
            return new SuccessDataResult<List<ProductMarketplaceStatusDto>>(statuses);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pazaryeri durum kartları yüklenirken hata. ProductId={ProductId}", productId);
            await applicationLogManager.AddLog("Pazaryeri durum bilgileri yüklenemedi.",
                LogType.Marketplace, LogAction.List);
            return new ErrorDataResult<List<ProductMarketplaceStatusDto>>([], "Pazaryeri durum bilgileri yüklenemedi.");
        }
    }

    public async Task<IDataResult<List<ProductActivityLog>>> GetTimelineAsync(
        Guid productId, ProductActivityTimelineFilter filter, int pageSize = 20, bool? ecommerceEnabled = null)
    {
        if (!(ecommerceEnabled ?? await IsEcommerceEnabledAsync()))
        {
            logger.LogDebug("E-ticaret kapalı; aktivite timeline atlandı. ProductId={ProductId}", productId);
            return new SuccessDataResult<List<ProductActivityLog>>([]);
        }

        try
        {
            return await activityLogger.GetTimelineAsync(productId, filter, pageSize);
        }
        catch (Exception ex)
        {
            // Diğer sekmelerle tutarlı graceful degrade — timeline sorgusu patlarsa HTMX partial'ı
            // 500 ile kırılmasın, boş/hata sonucu dönsün.
            logger.LogError(ex, "Aktivite timeline yüklenirken hata. ProductId={ProductId}", productId);
            await applicationLogManager.AddLog("Aktivite geçmişi yüklenemedi.", LogType.Product, LogAction.List);
            return new ErrorDataResult<List<ProductActivityLog>>([], "Aktivite geçmişi yüklenemedi.");
        }
    }

    public async Task<IDataResult<List<ProductOrderReferenceDto>>> GetOrdersAsync(Guid productId, int limit = 20)
    {
        if (limit <= 0) limit = 20;

        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            // OrderItem.ProductId aslında ProductVariant.Id'sine işaret eder (navigation: Product → ProductVariant).
            // Bu ürünün siparişleri = variant'ları üzerinden eşleşen OrderItem'lar.
            var rows = await dbContext.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Product != null && oi.Product.ProductId == productId)
                .OrderByDescending(oi => oi.Order.OrderDate ?? oi.Order.CreatedAt)
                .Take(limit)
                .Select(oi => new
                {
                    oi.OrderId,
                    oi.Order.OrderNumber,
                    OrderDate = oi.Order.OrderDate ?? oi.Order.CreatedAt,
                    MarketplaceName = oi.Order.MarketPlace != null ? oi.Order.MarketPlace.Name : null,
                    oi.ProductColor,
                    oi.ProductSize,
                    VariantName = oi.Product!.Name,
                    oi.Quantity
                })
                .ToListAsync();

            var orders = rows
                .Select(r => new ProductOrderReferenceDto(
                    r.OrderId,
                    r.OrderNumber,
                    r.OrderDate,
                    r.MarketplaceName ?? "Mağaza",
                    BuildVariantLabel(r.ProductColor, r.ProductSize, r.VariantName),
                    r.Quantity))
                .ToList();

            logger.LogInformation("Ürün siparişleri yüklendi. ProductId={ProductId}, Count={Count}",
                productId, orders.Count);
            return new SuccessDataResult<List<ProductOrderReferenceDto>>(orders);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ürün siparişleri yüklenirken hata. ProductId={ProductId}", productId);
            await applicationLogManager.AddLog("Ürün siparişleri yüklenemedi.", LogType.Order, LogAction.List);
            return new ErrorDataResult<List<ProductOrderReferenceDto>>([], "Ürün siparişleri yüklenemedi.");
        }
    }

    public async Task<IDataResult<List<ProductStockMovementDto>>> GetStockMovementsAsync(Guid productId, int limit = 100)
    {
        if (limit <= 0) limit = 100;

        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            var movements = await dbContext.StockMovements
                .AsNoTracking()
                .Where(sm => sm.ProductVariant.ProductId == productId)
                .OrderByDescending(sm => sm.CreatedAt)
                .Take(limit)
                .Select(sm => new ProductStockMovementDto(
                    sm.ProductVariantId,
                    sm.ProductVariant.Name,
                    sm.Type,
                    sm.Quantity,
                    sm.StockBefore,
                    sm.StockAfter,
                    sm.ReferenceType,
                    sm.ReferenceId,
                    sm.Note,
                    sm.CreatedAt))
                .ToListAsync();

            logger.LogInformation("Ürün stok hareketleri yüklendi. ProductId={ProductId}, Count={Count}",
                productId, movements.Count);
            return new SuccessDataResult<List<ProductStockMovementDto>>(movements);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ürün stok hareketleri yüklenirken hata. ProductId={ProductId}", productId);
            await applicationLogManager.AddLog("Ürün stok hareketleri yüklenemedi.", LogType.StockSync, LogAction.List);
            return new ErrorDataResult<List<ProductStockMovementDto>>([], "Ürün stok hareketleri yüklenemedi.");
        }
    }

    private static string? BuildVariantLabel(string? color, string? size, string? variantName)
    {
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(color)) parts.Add(color!.Trim());
        if (!string.IsNullOrWhiteSpace(size)) parts.Add(size!.Trim());

        if (parts.Count > 0)
            return string.Join(" / ", parts);

        return string.IsNullOrWhiteSpace(variantName) ? null : variantName!.Trim();
    }
}
