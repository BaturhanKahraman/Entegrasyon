using System.Diagnostics;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
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
    EventChannel<ProductCreatedForMarketplaceEvent> eventChannel,
    IApplicationLogManager applicationLogManager,
    IProductActivityLogger activityLogger,
    ITenantContext tenantContext) : IProductSyncManager
{
    private const long AdvisoryLockKeySyncAll = 1001;
    private const long AdvisoryLockKeyRetryFailed = 1002;
    public async Task<ProductSyncSummaryDto> GetSyncSummaryAsync(int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
        var product = await dbContext.MainProducts
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                p.Id,
                p.Title,
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
                mp.Id, mp.Name, state,
                record?.LastSyncedAt, record?.BatchRequestId, record?.StatusMessage);
        }).ToList();

        var dto = new ProductSyncDetailDto(
            product.Id, product.Title, product.StockCode!,
            product.BrandName, product.CategoryName, product.VariantCount,
            marketplaceItems);

        return new SuccessDataResult<ProductSyncDetailDto>(dto);
    }

    public async Task<IResult> SyncProductAsync(Guid productId, int marketPlaceId)
    {
        var marketplaceName = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
        using var activity = EntegrasyonActivitySource.StartMarketplaceOperation(marketplaceName, "QueueSync");
        activity?.SetTag("product.id", productId.ToString());

        using var dbContext = contextFactory.CreateDbContext();
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

        await dbContext.SaveChangesAsync();

        await eventChannel.PublishAsync(new ProductCreatedForMarketplaceEvent(productId, [marketplaceName])
        {
            TenantId = tenantContext.TenantId
        });

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
        using var dbContext = contextFactory.CreateDbContext();
        var marketplace = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlaceId == marketPlaceId);

        if (marketplace is null)
            return new ErrorResult("Pazaryeri kaydı bulunamadı.");

        if (marketplace.Status is not (MarketplaceProductStatus.Failed or MarketplaceProductStatus.Rejected))
            return new ErrorResult("Sadece başarısız veya reddedilmiş ürünler tekrarlanabilir.");

        marketplace.Status = MarketplaceProductStatus.Pending;
        marketplace.BatchRequestId = null;
        marketplace.StatusMessage = null;
        await dbContext.SaveChangesAsync();

        var marketplaceName = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
        await eventChannel.PublishAsync(new ProductCreatedForMarketplaceEvent(productId, [marketplaceName])
        {
            TenantId = tenantContext.TenantId
        });

        await activityLogger.LogAsync(productId, ProductActivityType.PublishRequested,
            $"{marketplaceName} için yeniden kuyruğa eklendi",
            ProductActivityStatus.Info, marketplaceName: marketplaceName);

        return new SuccessResult("Ürün yeniden kuyruğa eklendi.");
    }

    public async Task<IResult> SyncAllPendingAsync(int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();

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

            foreach (var productId in unsyncedProductIds)
            {
                dbContext.ProductMarketplaces.Add(new ProductMarketplace
                {
                    ProductId = productId,
                    MarketPlaceId = marketPlaceId,
                    Status = MarketplaceProductStatus.Pending
                });
            }

            await dbContext.SaveChangesAsync();

            var marketplaceName = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
            foreach (var productId in unsyncedProductIds)
            {
                await eventChannel.PublishAsync(new ProductCreatedForMarketplaceEvent(productId, [marketplaceName])
                {
                    TenantId = tenantContext.TenantId
                });
            }

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
        using var dbContext = contextFactory.CreateDbContext();

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

            foreach (var record in failedRecords)
            {
                record.Status = MarketplaceProductStatus.Pending;
                record.BatchRequestId = null;
                record.StatusMessage = null;
            }

            await dbContext.SaveChangesAsync();

            var marketplaceName = marketPlaceId == 1 ? "Trendyol" : $"Marketplace-{marketPlaceId}";
            foreach (var record in failedRecords)
            {
                await eventChannel.PublishAsync(new ProductCreatedForMarketplaceEvent(record.ProductId, [marketplaceName])
                {
                    TenantId = tenantContext.TenantId
                });
            }

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
