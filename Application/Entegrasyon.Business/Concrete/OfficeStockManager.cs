using Entegrasyon.Business.Extensions;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public class OfficeStockManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IBranchOfficeManager branchOfficeManager,
    IProductVariantManager productVariantManager,
    INotificationManager notificationManager,
    EventChannel<StockPriceChangedEvent> stockPriceChannel,
    ITenantContext tenantContext,
    ILogger<OfficeStockManager> logger) : IOfficeStockManager
{
    public async Task<IResult> AddOfficeStocks(IEnumerable<BranchOfficeStock> stocks)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var enumeratedStocks = stocks.ToList();
        var result = LogicRunner.Run(await CheckIfOfficeExists(enumeratedStocks.Select(x => x.BranchOfficeId).ToArray()));
        if (result != null)
            return result;
        dbContext.BranchOfficeStocks.AddRange(enumeratedStocks);
        await dbContext.SaveChangesAsync();
        return new SuccessResult();
    }

    public async Task<IResult> CheckIfOfficeExists(IEnumerable<int> officesIds)
    {
        var result = await branchOfficeManager.CheckIfOfficesExits(officesIds);
        return result ? new SuccessResult() : new ErrorResult("Bir veya daha fazla ofis bulunamadı");
    }

    public async Task UpdateStock(int branchOfficeId, Guid productVariantId, int stock)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var stockToUpdate = await dbContext.BranchOfficeStocks.AsTracking()
            .FirstOrDefaultAsync(x => x.BranchOfficeId == branchOfficeId && x.ProductVariantId == productVariantId);
        stockToUpdate!.FirstTotalStock = stock;
        await dbContext.SaveChangesAsync();
    }

    public IResult CheckIfProductCountZero(params AddBranchOfficeStockDto[] stocks)
    {
        if (stocks == null)
            return new SuccessResult();
        var stocksList = stocks.ToList();
        if (stocksList.All(x => x.FirstTotalStock == 0))
            return new ErrorResult("Lütfen en az bir stok girin.");
        return new SuccessResult();
    }

    public async Task<IResult> DecreaseProductStock(Guid id, int stockNumber, int branchId, bool overrideStockStatus = false)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var productVariant = await productVariantManager.GetById(id);
        if (productVariant == null)
            return new ErrorResult("İlettiğiniz ürün bulunamamıştır.");
        var stockStatus = await dbContext.BranchOfficeStocks.AsTracking()
            .FirstOrDefaultAsync(b => b.ProductVariantId == id && b.BranchOfficeId == branchId);
        if (stockStatus == null)
            return new ErrorResult("İlettiğiniz ürünün bu ofis/depoda stoğu bulunamamıştır.");
        if (!overrideStockStatus && stockStatus.CurrentStock < stockNumber)
            return new ErrorResult("İlettiğiniz ofiste/depoda yeterli stok bulunmamaktadır.");
        stockStatus.SoldQuantity += stockNumber;
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Stok başarı ile düşmüştür.");
    }

    public async Task<IResult> DecreaseProductsStock(List<DecreaseStockDto> dtos, bool overrideStockStatus = false)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var officeIds = dtos.Select(x => x.OfficeId).ToList();
        var productIds = dtos.Select(x => x.ProductId).ToList();
        var dbStocks = await dbContext.BranchOfficeStocks.AsTracking()
            .Where(s => officeIds.Contains(s.BranchOfficeId) && s.ProductVariantId.HasValue && productIds.Contains(s.ProductVariantId.Value))
            .ToListAsync();

        foreach (var dto in dtos)
        {
            var officeStock = dbStocks.First(s => dto.ProductId == s.ProductVariantId && s.FirstTotalStock > 0);
            if (!overrideStockStatus && officeStock.CurrentStock < dto.StockNumber)
                return new ErrorResult(dto.ProductId + " id li üründe stok yetersiz.");
            officeStock.SoldQuantity += dto.StockNumber;
        }
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Başarıyla stoktan düşüldü.");
    }

    public async Task<IDataResult<StockMovement>> DecreaseStockAtomicAsync(
        int branchOfficeId, Guid productVariantId, int quantity,
        StockMovementType type, string? referenceType = null, string? referenceId = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // Atomic update — tek SQL: UPDATE SET SoldQuantity = SoldQuantity + @qty WHERE CurrentStock >= @qty
        var affected = await dbContext.BranchOfficeStocks
            .Where(s => s.BranchOfficeId == branchOfficeId
                && s.ProductVariantId == productVariantId
                && s.CurrentStock >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.SoldQuantity, b => b.SoldQuantity + quantity));

        if (affected == 0)
            return new ErrorDataResult<StockMovement>(null!, "Yetersiz stok — ürün başka bir kanaldan satılmış olabilir.");

        // Güncel stoku oku ve StockMovement kaydı oluştur
        var currentStock = await dbContext.BranchOfficeStocks.AsNoTracking()
            .Where(s => s.BranchOfficeId == branchOfficeId && s.ProductVariantId == productVariantId)
            .Select(s => s.CurrentStock)
            .FirstOrDefaultAsync();

        var movement = new StockMovement
        {
            BranchOfficeId = branchOfficeId,
            ProductVariantId = productVariantId,
            Type = type,
            Quantity = -quantity,
            StockBefore = currentStock + quantity,
            StockAfter = currentStock,
            ReferenceType = referenceType,
            ReferenceId = referenceId
        };
        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync();

        // Stok seviye kontrolü + marketplace sync event
        await CheckStockLevelsAsync(branchOfficeId, productVariantId, currentStock);

        return new SuccessDataResult<StockMovement>(movement, "Stok başarıyla düşüldü.");
    }

    public async Task<IDataResult<StockMovement>> ForceDecreaseStockAsync(
        int branchOfficeId, Guid productVariantId, int quantity,
        StockMovementType type, string? referenceType = null, string? referenceId = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // Stok yetersiz olsa bile düş — WHERE'de CurrentStock kontrolü yok
        await dbContext.BranchOfficeStocks
            .Where(s => s.BranchOfficeId == branchOfficeId
                && s.ProductVariantId == productVariantId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.SoldQuantity, b => b.SoldQuantity + quantity));

        var currentStock = await dbContext.BranchOfficeStocks.AsNoTracking()
            .Where(s => s.BranchOfficeId == branchOfficeId && s.ProductVariantId == productVariantId)
            .Select(s => s.CurrentStock)
            .FirstOrDefaultAsync();

        var movement = new StockMovement
        {
            BranchOfficeId = branchOfficeId,
            ProductVariantId = productVariantId,
            Type = type,
            Quantity = -quantity,
            StockBefore = currentStock + quantity,
            StockAfter = currentStock,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Note = currentStock < 0 ? "Negatif stok — marketplace zorunlu düşüş" : null
        };
        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync();

        await CheckStockLevelsAsync(branchOfficeId, productVariantId, currentStock);

        return new SuccessDataResult<StockMovement>(movement, "Stok zorla düşüldü.");
    }

    public async Task<IDataResult<StockMovement>> IncreaseStockAtomicAsync(
        int branchOfficeId, Guid productVariantId, int quantity,
        StockMovementType type, string? referenceType = null, string? referenceId = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Atomic update — SoldQuantity azalt (stok geri ver)
        var affected = await dbContext.BranchOfficeStocks
            .Where(s => s.BranchOfficeId == branchOfficeId
                && s.ProductVariantId == productVariantId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.SoldQuantity, b => b.SoldQuantity - quantity));

        if (affected == 0)
            return new ErrorDataResult<StockMovement>(null!, "Stok kaydi bulunamadı.");

        var currentStock = await dbContext.BranchOfficeStocks.AsNoTracking()
            .Where(s => s.BranchOfficeId == branchOfficeId && s.ProductVariantId == productVariantId)
            .Select(s => s.CurrentStock)
            .FirstOrDefaultAsync();

        var movement = new StockMovement
        {
            BranchOfficeId = branchOfficeId,
            ProductVariantId = productVariantId,
            Type = type,
            Quantity = quantity,
            StockBefore = currentStock - quantity,
            StockAfter = currentStock,
            ReferenceType = referenceType,
            ReferenceId = referenceId
        };
        dbContext.StockMovements.Add(movement);
        await dbContext.SaveChangesAsync();

        await CheckStockLevelsAsync(branchOfficeId, productVariantId, currentStock);

        return new SuccessDataResult<StockMovement>(movement, "Stok basariyla geri verildi.");
    }

    public async Task<IDataResult<StockTransferResultDto>> TransferStockAsync(
        int sourceBranchId, int targetBranchId, List<TransferItemDto> items)
    {
        // Validation
        if (sourceBranchId == targetBranchId)
            return new ErrorDataResult<StockTransferResultDto>(null!, "Kaynak ve hedef depo ayni olamaz.");

        if (items.Any(i => i.Quantity <= 0))
            return new ErrorDataResult<StockTransferResultDto>(null!, "Transfer miktari 0'dan buyuk olmalidir.");

        // Check target branch is active
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var targetBranch = await dbContext.BranchOffices
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == targetBranchId && !b.IsDeleted);

        if (targetBranch is null)
            return new ErrorDataResult<StockTransferResultDto>(null!, "Hedef depo aktif degil veya bulunamadı.");

        // Pre-check: yeterli stok var mi? (ExecuteUpdateAsync'ten once kontrol — unit test dostu)
        // CurrentStock computed column yerine FirstTotalStock - SoldQuantity kullanilir
        foreach (var item in items)
        {
            var hasEnough = await dbContext.BranchOfficeStocks
                .AsNoTracking()
                .AnyAsync(s => s.BranchOfficeId == sourceBranchId
                    && s.ProductVariantId == item.ProductVariantId
                    && (s.FirstTotalStock - s.SoldQuantity) >= item.Quantity);
            if (!hasEnough)
                return new ErrorDataResult<StockTransferResultDto>(null!,
                    $"Yetersiz stok: {item.ProductVariantId}");
        }

        var sourceMovements = new List<StockMovement>();
        var targetMovements = new List<StockMovement>();

        // Process each item — decrease source, increase target
        foreach (var item in items)
        {
            // Decrease source atomically
            var decreaseResult = await DecreaseStockAtomicAsync(
                sourceBranchId, item.ProductVariantId, item.Quantity,
                StockMovementType.Transfer,
                referenceType: "Transfer",
                referenceId: targetBranchId.ToString());

            if (!decreaseResult.Success)
                return new ErrorDataResult<StockTransferResultDto>(null!, decreaseResult.Message!);

            sourceMovements.Add(decreaseResult.Data!);

            // Increase target atomically
            var increaseResult = await IncreaseStockAtomicAsync(
                targetBranchId, item.ProductVariantId, item.Quantity,
                StockMovementType.Transfer,
                referenceType: "Transfer",
                referenceId: sourceBranchId.ToString());

            if (!increaseResult.Success)
                return new ErrorDataResult<StockTransferResultDto>(null!, increaseResult.Message!);

            targetMovements.Add(increaseResult.Data!);
        }

        // Publish stock changed events for both branches
        foreach (var item in items)
        {
            await using var ctx = await contextFactory.CreateDbContextAsync();
            var productId = await ctx.ProductVariants.AsNoTracking()
                .Where(v => v.Id == item.ProductVariantId)
                .Select(v => v.ProductId)
                .FirstOrDefaultAsync();

            stockPriceChannel.TryPublish(new StockPriceChangedEvent(item.ProductVariantId, productId)
            {
                TenantId = tenantContext.TenantId
            });
        }

        var transferResult = new StockTransferResultDto(items.Count, sourceMovements, targetMovements);
        return new SuccessDataResult<StockTransferResultDto>(transferResult,
            $"{items.Count} ürün başarıyla transfer edildi.");
    }

    public async Task<IDataResult<Pageable<StockMovementViewDto>>> GetStockMovementsAsync(
        int pageIndex = 0, int pageSize = 50,
        int? branchOfficeId = null, StockMovementType? type = null)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = dbContext.StockMovements
            .Include(m => m.ProductVariant).ThenInclude(pv => pv.Product)
            .AsNoTracking()
            .AsQueryable();

        if (branchOfficeId.HasValue)
            query = query.Where(m => m.BranchOfficeId == branchOfficeId.Value);
        if (type.HasValue)
            query = query.Where(m => m.Type == type.Value);

        var totalCount = await query.CountAsync();
        var raw = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.CreatedAt,
                ProductTitle = m.ProductVariant.Product!.Title ?? "",
                VariantName = m.ProductVariant.Name,
                Barcode = m.ProductVariant.Barcode ?? "",
                m.Type,
                m.Quantity,
                m.StockBefore,
                m.StockAfter,
                m.ReferenceType,
                m.ReferenceId,
                RawAttrs = m.ProductVariant.ProductVariantAttributes.Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer }).ToList()
            })
            .ToListAsync();

        var items = raw.Select(r => new StockMovementViewDto(
            r.Id,
            r.CreatedAt,
            r.ProductTitle,
            VariantNameExtensions.ResolveDisplayName(
                r.VariantName,
                r.RawAttrs.Select((a, i) => new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
                r.ProductTitle),
            r.Barcode,
            r.Type,
            r.Quantity,
            r.StockBefore,
            r.StockAfter,
            r.ReferenceType,
            r.ReferenceId
        )).ToList();

        return new SuccessDataResult<Pageable<StockMovementViewDto>>(
            new Pageable<StockMovementViewDto>(items, pageIndex, pageSize, totalCount));
    }

    public async Task<int> GetAvailableStockAsync(int branchOfficeId, Guid productVariantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.BranchOfficeStocks.AsNoTracking()
            .Where(s => s.BranchOfficeId == branchOfficeId && s.ProductVariantId == productVariantId)
            .Select(s => s.CurrentStock)
            .FirstOrDefaultAsync();
    }

    public Task PublishStockChangedEventAsync(Guid productVariantId, Guid productId)
    {
        stockPriceChannel.TryPublish(new StockPriceChangedEvent(productVariantId, productId)
        {
            TenantId = tenantContext.TenantId
        });
        return Task.CompletedTask;
    }

    private async Task CheckStockLevelsAsync(int branchOfficeId, Guid productVariantId, int currentStock)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // ProductId'yi al (marketplace sync event için gerekli)
        var productId = await dbContext.ProductVariants.AsNoTracking()
            .Where(v => v.Id == productVariantId)
            .Select(v => v.ProductId)
            .FirstOrDefaultAsync();

        // Stok değişti → marketplace sync event yayınla.
        // Tenant context HTTP request dışında (background servis, integration test, storefront checkout)
        // initialize edilmemiş olabilir — IsInitialized guard ile güvenli yayınla.
        if (tenantContext.IsInitialized)
        {
            stockPriceChannel.TryPublish(new StockPriceChangedEvent(productVariantId, productId)
            {
                TenantId = tenantContext.TenantId
            });
        }
        else
        {
            logger.LogDebug(
                "CheckStockLevels: tenant context başlatılmamış, marketplace sync event atlandı. " +
                "variant={VariantId}", productVariantId);
        }

        // Admin kullanıcılarının ID'lerini al (bildirimleri onlara gönder).
        // Boş liste SendNotification validator'ını patlatır — guard ile sadece kullanıcı varsa gönder.
        var adminUserIds = await dbContext.Users.AsNoTracking()
            .Select(u => u.Id)
            .ToListAsync();

        if (adminUserIds.Count == 0)
            return;

        if (currentStock < 0)
        {
            logger.LogWarning("Negatif stok! VariantId={VariantId}, BranchId={BranchId}, Stock={Stock}",
                productVariantId, branchOfficeId, currentStock);

            await notificationManager.SendNotification(
                "Negatif Stok Uyarısı",
                $"Ürün stoku negatife düştü ({currentStock}). Marketplace'lerde stok 0'a çekilecek.",
                NotificationSeverity.Error,
                NotificationCategory.Stok,
                adminUserIds);
        }
        else if (currentStock == 0)
        {
            await notificationManager.SendNotification(
                "Stok Tükendi",
                $"Ürün stoğu tamamen tükendi. Marketplace'lerde stok 0 olarak güncellenecek.",
                NotificationSeverity.Warning,
                NotificationCategory.Stok,
                adminUserIds);
        }
        else if (currentStock <= 5)
        {
            await notificationManager.SendNotification(
                "Düşük Stok Uyarısı",
                $"Ürün stoğu kritik seviyeye düştü: {currentStock} adet kaldı.",
                NotificationSeverity.Info,
                NotificationCategory.Stok,
                adminUserIds);
        }
    }
}
