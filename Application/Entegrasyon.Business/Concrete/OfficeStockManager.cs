using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
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
    ILogger<OfficeStockManager> logger) : IOfficeStockManager
{
    public async Task<IResult> AddOfficeStocks(IEnumerable<BranchOfficeStock> stocks)
    {
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();

        // Atomic update — SoldQuantity azalt (stok geri ver)
        var affected = await dbContext.BranchOfficeStocks
            .Where(s => s.BranchOfficeId == branchOfficeId
                && s.ProductVariantId == productVariantId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.SoldQuantity, b => b.SoldQuantity - quantity));

        if (affected == 0)
            return new ErrorDataResult<StockMovement>(null!, "Stok kaydi bulunamadi.");

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

    private async Task CheckStockLevelsAsync(int branchOfficeId, Guid productVariantId, int currentStock)
    {
        using var dbContext = contextFactory.CreateDbContext();
        // ProductId'yi al (marketplace sync event için gerekli)
        var productId = await dbContext.ProductVariants.AsNoTracking()
            .Where(v => v.Id == productVariantId)
            .Select(v => v.ProductId)
            .FirstOrDefaultAsync();

        // Stok değişti → marketplace sync event yayınla
        stockPriceChannel.TryPublish(new StockPriceChangedEvent(productVariantId, productId));

        // Admin kullanıcılarının ID'lerini al (bildirimleri onlara gönder)
        var adminUserIds = await dbContext.Users.AsNoTracking()
            .Select(u => u.Id)
            .ToListAsync();

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
