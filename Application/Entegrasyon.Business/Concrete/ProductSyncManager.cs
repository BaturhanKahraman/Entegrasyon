using System.Diagnostics;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Diagnostics;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class ProductSyncManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IProductActivityLogger activityLogger,
    ITenantContext tenantContext) : IProductSyncManager
{
    private const long AdvisoryLockKeySyncAll = 1001;
    private const long AdvisoryLockKeyRetryFailed = 1002;
    public async Task<ProductSyncSummaryDto> GetSyncSummaryAsync(int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var totalProducts = await dbContext.MainProducts.CountAsync();

        var statusCounts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == marketPlaceId)
            .GroupBy(pm => pm.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var syncedCount = statusCounts
            .Where(s => s.Status == MarketplaceProductStatus.Published)
            .Sum(s => s.Count);
        var pendingCount = statusCounts
            .Where(s => s.Status == MarketplaceProductStatus.Pending)
            .Sum(s => s.Count);
        var failedCount = statusCounts
            .Where(s => s.Status is MarketplaceProductStatus.Failed or MarketplaceProductStatus.Rejected)
            .Sum(s => s.Count);
        var neverSyncedCount = totalProducts - syncedCount - pendingCount - failedCount;

        return new ProductSyncSummaryDto(totalProducts, syncedCount, pendingCount, failedCount, neverSyncedCount);
    }

    public async Task<DataResult<Pageable<ProductSyncListItemDto>>> GetProductSyncListAsync(
        int marketPlaceId, MarketplaceSyncState? stateFilter, string searchKey, int pageIndex, int pageSize)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = dbContext.MainProducts
            .Select(p => new
            {
                Product = p,
                Marketplace = p.ProductMarketplaces
                    .FirstOrDefault(pm => pm.MarketPlaceId == marketPlaceId)
            });

        // Arama filtresi
        if (!string.IsNullOrWhiteSpace(searchKey))
        {
            var search = searchKey.ToLower();
            query = query.Where(x =>
                x.Product.Title.ToLower().Contains(search) ||
                x.Product.StockCode!.ToLower().Contains(search));
        }

        // Durum filtresi
        if (stateFilter.HasValue)
        {
            query = stateFilter.Value switch
            {
                MarketplaceSyncState.NeverSynced => query.Where(x => x.Marketplace == null),
                MarketplaceSyncState.Waiting => query.Where(x =>
                    x.Marketplace != null &&
                    x.Marketplace.Status == MarketplaceProductStatus.Pending &&
                    x.Marketplace.BatchRequestId == null),
                MarketplaceSyncState.Processing => query.Where(x =>
                    x.Marketplace != null &&
                    x.Marketplace.Status == MarketplaceProductStatus.Pending &&
                    x.Marketplace.BatchRequestId != null),
                MarketplaceSyncState.Synced => query.Where(x =>
                    x.Marketplace != null &&
                    x.Marketplace.Status == MarketplaceProductStatus.Published &&
                    x.Product.UpdatedAt <= x.Marketplace.LastSyncedAt),
                MarketplaceSyncState.OutOfSync => query.Where(x =>
                    x.Marketplace != null &&
                    x.Marketplace.Status == MarketplaceProductStatus.Published &&
                    x.Product.UpdatedAt > x.Marketplace.LastSyncedAt),
                MarketplaceSyncState.Failed => query.Where(x =>
                    x.Marketplace != null &&
                    x.Marketplace.Status == MarketplaceProductStatus.Failed),
                MarketplaceSyncState.Rejected => query.Where(x =>
                    x.Marketplace != null &&
                    x.Marketplace.Status == MarketplaceProductStatus.Rejected),
                _ => query
            };
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Product.Title)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Product.Id,
                x.Product.Title,
                x.Product.StockCode,
                BrandName = x.Product.Brand != null ? x.Product.Brand.Name : "",
                CategoryName = x.Product.Category != null ? x.Product.Category.Name : "",
                VariantCount = x.Product.ProductVariants.Count(),
                x.Product.UpdatedAt,
                Marketplace = x.Marketplace
            })
            .ToListAsync();

        var dtoItems = items.Select(x => new ProductSyncListItemDto(
            x.Id, x.Title, x.StockCode!, x.BrandName, x.CategoryName, x.VariantCount,
            MapSyncState(x.Marketplace, x.UpdatedAt),
            x.Marketplace?.LastSyncedAt,
            x.Marketplace?.StatusMessage)).ToList();

        var pageable = new Pageable<ProductSyncListItemDto>(dtoItems, pageIndex, pageSize, totalCount);
        return new SuccessDataResult<Pageable<ProductSyncListItemDto>>(pageable);
    }

    public async Task<IDataResult<ProductSyncDetailDto>> GetProductSyncDetailAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.StockCode,
                BrandName = p.Brand != null ? p.Brand.Name : "",
                CategoryName = p.Category != null ? p.Category.Name : "",
                VariantCount = p.ProductVariants.Count(),
                p.UpdatedAt,
                MarketplaceRecords = p.ProductMarketplaces.ToList()
            })
            .FirstOrDefaultAsync();

        if (product is null)
            return new ErrorDataResult<ProductSyncDetailDto>(null!, "Ürün bulunamadı.");

        var allMarketplaces = await dbContext.MarketPlaces.ToListAsync();

        var marketplaceItems = allMarketplaces.Select(mp =>
        {
            var record = product.MarketplaceRecords.FirstOrDefault(r => r.MarketPlaceId == mp.Id);
            var state = MapSyncState(record, product.UpdatedAt);
            return new MarketplaceSyncItemDto(
                MarketPlaceId: mp.Id,
                MarketPlaceName: mp.Name,
                SyncState: state,
                LastSyncedAt: record?.LastSyncedAt,
                BatchRequestId: record?.BatchRequestId,
                StatusMessage: record?.StatusMessage,
                ExternalProductId: record?.ExternalProductId,
                ContentId: record?.ContentId,
                IsApproved: record?.IsApproved,
                IsArchived: record?.IsArchived,
                HasCredentials: HasMarketplaceCredentials(mp));
        }).ToList();

        var dto = new ProductSyncDetailDto(
            product.Id, product.Title, product.Description, product.StockCode!,
            product.BrandName, product.CategoryName, product.VariantCount,
            marketplaceItems);

        return new SuccessDataResult<ProductSyncDetailDto>(dto);
    }

    /// <summary>
    /// Credential guard: pazaryeri API anahtarları eksikse sync reddedilir.
    /// UI disable'ı bypass eden doğrudan POST'lara karşı backend koruması (spec #81).
    /// Tek kaynak: <see cref="MarketPlaceCredentialExtensions.IsCredentialComplete"/>.
    /// </summary>
    private async Task<IResult?> RejectIfCredentialsMissingAsync(
        IntegrationDbContext dbContext, int marketPlaceId, string marketplaceName)
    {
        var mp = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == marketPlaceId);
        if (mp is not null && mp.IsCredentialComplete())
            return null;

        await applicationLogManager.AddLog(
            $"{marketplaceName} senkronizasyonu reddedildi: API anahtarları eksik.",
            LogType.Marketplace, LogAction.Sync, new { marketPlaceId });
        return new ErrorResult(
            "Bu pazaryeri için API anahtarları eksik. Önce Ayarlar → Entegrasyonlar'dan tamamlayın.");
    }

    public async Task<IResult> SyncProductAsync(Guid productId, int marketPlaceId)
    {
        var marketplaceName = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
        using var activity = EntegrasyonActivitySource.StartMarketplaceOperation(marketplaceName, "QueueSync");
        activity?.SetTag("product.id", productId.ToString());

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var credentialGuard = await RejectIfCredentialsMissingAsync(dbContext, marketPlaceId, marketplaceName);
        if (credentialGuard is not null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "API anahtarları eksik");
            return credentialGuard;
        }

        var product = await dbContext.MainProducts.FindAsync(productId);
        if (product is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Ürün bulunamadı");
            return new ErrorResult("Ürün bulunamadı.");
        }

        var marketplace = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlaceId == marketPlaceId);

        if (marketplace is null)
        {
            marketplace = new ProductMarketplace
            {
                ProductId = productId,
                MarketPlaceId = marketPlaceId,
                Status = MarketplaceProductStatus.Pending
            };
            dbContext.ProductMarketplaces.Add(marketplace);
        }
        else
        {
            marketplace.Status = MarketplaceProductStatus.Pending;
            marketplace.BatchRequestId = null;
            marketplace.StatusMessage = null;
        }

        dbContext.AddDomainEvent(new ProductCreatedForMarketplaceEvent(productId, [marketplaceName])
        {
            TenantId = tenantContext.TenantId
        });
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.PublishRequested,
            $"{marketplaceName} senkronizasyon kuyruğuna eklendi",
            ProductActivityStatus.Info, marketplaceName: marketplaceName);
        await applicationLogManager.AddLog(
            $"Ürün {marketplaceName} senkronizasyonuna gönderildi",
            LogType.Marketplace, LogAction.Sync, new { productId, marketplaceName });

        activity?.SetStatus(ActivityStatusCode.Ok);
        return new SuccessResult("Ürün senkronizasyon kuyruğuna eklendi.");
    }

    public async Task<IResult> RetryFailedAsync(Guid productId, int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketplace = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlaceId == marketPlaceId);

        if (marketplace is null)
            return new ErrorResult("Pazaryeri kaydı bulunamadı.");

        if (marketplace.Status is not (MarketplaceProductStatus.Failed or MarketplaceProductStatus.Rejected))
            return new ErrorResult("Sadece başarısız veya reddedilmiş ürünler tekrarlanabilir.");

        marketplace.Status = MarketplaceProductStatus.Pending;
        marketplace.BatchRequestId = null;
        marketplace.StatusMessage = null;

        var marketplaceName = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
        dbContext.AddDomainEvent(new ProductCreatedForMarketplaceEvent(productId, [marketplaceName])
        {
            TenantId = tenantContext.TenantId
        });
        await dbContext.SaveChangesAsync();

        await activityLogger.LogAsync(productId, ProductActivityType.PublishRequested,
            $"{marketplaceName} için yeniden kuyruğa eklendi",
            ProductActivityStatus.Info, marketplaceName: marketplaceName);

        return new SuccessResult("Ürün yeniden kuyruğa eklendi.");
    }

    public async Task<IResult> SyncAllPendingAsync(int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var credentialGuard = await RejectIfCredentialsMissingAsync(
            dbContext, marketPlaceId, marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}");
        if (credentialGuard is not null)
            return credentialGuard;

        // Advisory lock: Eşzamanlı toplu sync'i engelle
        var lockAcquired = await dbContext.Database
            .SqlQuery<bool>($"""SELECT pg_try_advisory_lock({AdvisoryLockKeySyncAll}) AS "Value" """)
            .FirstAsync();

        if (!lockAcquired)
            return new ErrorResult("Toplu senkronizasyon zaten devam ediyor.");

        var marketplaceNameForBulk = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
        using var bulkActivity = EntegrasyonActivitySource.StartBulkOperation("SyncAllPending", 0);
        bulkActivity?.SetTag("marketplace.name", marketplaceNameForBulk);

        try
        {
            var unsyncedProductIds = await dbContext.MainProducts
                .Where(p => !p.ProductMarketplaces.Any(pm => pm.MarketPlaceId == marketPlaceId))
                .Select(p => p.Id)
                .ToListAsync();

            var marketplaceName = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
            foreach (var productId in unsyncedProductIds)
            {
                dbContext.ProductMarketplaces.Add(new ProductMarketplace
                {
                    ProductId = productId,
                    MarketPlaceId = marketPlaceId,
                    Status = MarketplaceProductStatus.Pending
                });
                dbContext.AddDomainEvent(new ProductCreatedForMarketplaceEvent(productId, [marketplaceName])
                {
                    TenantId = tenantContext.TenantId
                });
            }

            await dbContext.SaveChangesAsync();

            await applicationLogManager.AddLog(
                $"{unsyncedProductIds.Count} ürün {marketplaceName} senkronizasyonuna toplu gönderildi",
                LogType.Marketplace, LogAction.Sync, new { count = unsyncedProductIds.Count, marketplaceName });

            bulkActivity?.SetTag("bulk.item_count", unsyncedProductIds.Count);
            bulkActivity?.SetStatus(ActivityStatusCode.Ok);
            EntegrasyonMetrics.ProductSyncTotal.Add(unsyncedProductIds.Count,
                new KeyValuePair<string, object?>("marketplace", marketplaceName));

            return new SuccessResult($"{unsyncedProductIds.Count} ürün senkronizasyon kuyruğuna eklendi.");
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock({0})", AdvisoryLockKeySyncAll);
        }
    }

    public async Task<IResult> RetryAllFailedAsync(int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var credentialGuard = await RejectIfCredentialsMissingAsync(
            dbContext, marketPlaceId, marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}");
        if (credentialGuard is not null)
            return credentialGuard;

        // Advisory lock: Eşzamanlı toplu retry'ı engelle
        var lockAcquired = await dbContext.Database
            .SqlQuery<bool>($"""SELECT pg_try_advisory_lock({AdvisoryLockKeyRetryFailed}) AS "Value" """)
            .FirstAsync();

        if (!lockAcquired)
            return new ErrorResult("Toplu yeniden deneme işlemi zaten devam ediyor.");

        try
        {
            var failedRecords = await dbContext.ProductMarketplaces
                .Where(pm => pm.MarketPlaceId == marketPlaceId &&
                             (pm.Status == MarketplaceProductStatus.Failed || pm.Status == MarketplaceProductStatus.Rejected))
                .ToListAsync();

            var marketplaceName = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
            foreach (var record in failedRecords)
            {
                record.Status = MarketplaceProductStatus.Pending;
                record.BatchRequestId = null;
                record.StatusMessage = null;
                dbContext.AddDomainEvent(new ProductCreatedForMarketplaceEvent(record.ProductId, [marketplaceName])
                {
                    TenantId = tenantContext.TenantId
                });
            }

            await dbContext.SaveChangesAsync();

            await applicationLogManager.AddLog(
                $"{failedRecords.Count} hatalı ürün {marketplaceName} için yeniden kuyruğa eklendi",
                LogType.Marketplace, LogAction.Retry, new { count = failedRecords.Count, marketplaceName });

            return new SuccessResult($"{failedRecords.Count} hatalı ürün yeniden kuyruğa eklendi.");
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock({0})", AdvisoryLockKeyRetryFailed);
        }
    }

    public async Task<IDataResult<ProductSendPreflightDto>> GetSendPreflightAsync(Guid productId, int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var product = await dbContext.MainProducts
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.ProductVariants).ThenInclude(v => v.Images)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return new ErrorDataResult<ProductSendPreflightDto>(null!, "Ürün bulunamadı.");

        // 1. Kategori eşleştirmesi kontrol et — kanonik tablo CategoryMarketplaces
        //    (kategori eşleme wizard'ının yazdığı tablo; eski CategoryMarketPlaceMatches değil).
        var categoryMatch = await dbContext.CategoryMarketplaces
            .FirstOrDefaultAsync(cm => cm.CategoryId == product.CategoryId && cm.MarketPlaceId == marketPlaceId && cm.IsActive);

        var categoryMatched = categoryMatch is not null;
        var matchedCategoryName = categoryMatch?.MarketPlaceCategoryId.ToString();  // Store the marketplace category ID as name reference

        // 2. Marka eşleştirmesi kontrol et
        var brandMatch = await dbContext.BrandMarketPlaceMatches
            .FirstOrDefaultAsync(bm => bm.ApplicationBrandId == product.BrandId && bm.MarketPlaceId == marketPlaceId);

        var brandMatched = brandMatch is not null;
        var matchedBrandName = brandMatch?.MarketPlaceBrandId.ToString();  // Store the marketplace brand ID as name reference

        // 3. Zorunlu özellikler kontrol et
        var requiredAttributes = await dbContext.CategoryAttributeCategories
            .Where(cac => cac.CategoryId == product.CategoryId && cac.IsRequired)
            .Include(cac => cac.CategoryAttribute)
            .ToListAsync();

        var missingAttributes = new List<string>();
        foreach (var req in requiredAttributes)
        {
            var isMapped = await dbContext.CategoryAttributeMarketPlaceMatches
                .AnyAsync(campm => campm.ApplicationCategoryAttributeId == req.CategoryAttributeId && campm.MarketPlaceId == marketPlaceId);

            if (!isMapped)
                missingAttributes.Add(req.CategoryAttribute.CategoryAttributeHumanized ?? req.CategoryAttribute.CategoryAttributeKey ?? "Bilinmeyen Özellik");
        }

        var requiredAttributesMatched = missingAttributes.Count == 0;

        // 4. Varyant ve barcode kontrol et
        var hasVariants = product.ProductVariants.Count > 0;
        var allVariantsHaveBarcodes = hasVariants && product.ProductVariants.All(v => !string.IsNullOrEmpty(v.Barcode));

        // 5. Görsel kontrol (T112): görselsiz varyant Trendyol'a gönderilemez — gönderim sessizce
        //    "görsel eksik" ile patlıyordu. Her varyantın ≥1 görseli olmalı (bloklayıcı).
        var variantsWithoutImages = product.ProductVariants
            .Where(v => v.Images.Count(i => !string.IsNullOrEmpty(i.StorageKey)) == 0)
            .Select(v => v.Barcode ?? v.Id.ToString())
            .ToList();
        var allVariantsHaveImages = hasVariants && variantsWithoutImages.Count == 0;

        // 6. Stok kaynağı (depo) eşlemesi (T109): uyarı — bloklamaz ama 0-stok riskini bildirir.
        var hasWarehouseMapping = await dbContext.MarketPlaceWarehouses
            .AnyAsync(w => w.MarketPlaceId == marketPlaceId);
        var hasDefaultStockBranch = await dbContext.BranchOffices
            .AnyAsync(b => b.IsDefaultMarketPlaceStock);
        var stockSourceConfigured = hasWarehouseMapping || hasDefaultStockBranch;

        var allPassed = categoryMatched && brandMatched && requiredAttributesMatched
            && hasVariants && allVariantsHaveBarcodes && allVariantsHaveImages;

        var preflight = new ProductSendPreflightDto(
            categoryMatched,
            matchedCategoryName,
            brandMatched,
            matchedBrandName,
            requiredAttributesMatched,
            missingAttributes,
            hasVariants,
            allVariantsHaveBarcodes,
            allPassed,
            ProductCategoryId: product.CategoryId,
            ProductCategoryName: product.Category?.Name,
            ProductBrandId: product.BrandId,
            ProductBrandName: product.Brand?.Name,
            AllVariantsHaveImages: allVariantsHaveImages,
            VariantsWithoutImages: variantsWithoutImages,
            StockSourceConfigured: stockSourceConfigured
        );

        return new SuccessDataResult<ProductSendPreflightDto>(preflight);
    }

    private static bool HasMarketplaceCredentials(MarketPlace marketplace)
    {
        // Trendyol: requires SellerId, ApiKey, ApiSecret
        if (marketplace.Id == 1)
            return !string.IsNullOrEmpty(marketplace.SellerId)
                && !string.IsNullOrEmpty(marketplace.ApiKey)
                && !string.IsNullOrEmpty(marketplace.ApiSecret);

        // OAuth2-based marketplaces (Pazarama, Amazon)
        if (marketplace.Id == 5 || marketplace.Id == 4)
            return !string.IsNullOrEmpty(marketplace.ApiKey)
                && !string.IsNullOrEmpty(marketplace.ApiSecret)
                && !string.IsNullOrEmpty(marketplace.TokenUrl);

        // Default: requires ApiKey and ApiSecret (for other marketplaces)
        return !string.IsNullOrEmpty(marketplace.ApiKey)
            && !string.IsNullOrEmpty(marketplace.ApiSecret);
    }

    private static MarketplaceSyncState MapSyncState(ProductMarketplace? marketplace, DateTimeOffset productUpdatedAt)
    {
        if (marketplace is null)
            return MarketplaceSyncState.NeverSynced;

        return marketplace.Status switch
        {
            MarketplaceProductStatus.Pending when marketplace.BatchRequestId is null => MarketplaceSyncState.Waiting,
            MarketplaceProductStatus.Pending => MarketplaceSyncState.Processing,
            MarketplaceProductStatus.Published when productUpdatedAt > marketplace.LastSyncedAt => MarketplaceSyncState.OutOfSync,
            MarketplaceProductStatus.Published => MarketplaceSyncState.Synced,
            MarketplaceProductStatus.Failed => MarketplaceSyncState.Failed,
            MarketplaceProductStatus.Rejected => MarketplaceSyncState.Rejected,
            _ => MarketplaceSyncState.NeverSynced
        };
    }
}
