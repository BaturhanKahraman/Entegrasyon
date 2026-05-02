using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Stock;

public class OfflineSaleSyncManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    INegativeStockIncidentManager incidentManager,
    IApplicationLogManager applicationLogManager,
    ILogger<OfflineSaleSyncManager> logger) : IOfflineSaleSyncManager
{
    public async Task<IDataResult<OfflineSaleSyncResult>> SyncOneAsync(int tenantId, OfflineSaleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdempotencyKey))
            return new ErrorDataResult<OfflineSaleSyncResult>(default!, "IdempotencyKey gerekli.");
        if (dto.Items is null || dto.Items.Count == 0)
            return new ErrorDataResult<OfflineSaleSyncResult>(default!, "En az bir kalem gerekli.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 1. Idempotency check — daha önce sync edildiyse mevcut sonucu döndür
        var existing = await dbContext.Sales
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdempotencyKey == dto.IdempotencyKey);

        if (existing is not null)
        {
            return new SuccessDataResult<OfflineSaleSyncResult>(
                new OfflineSaleSyncResult(dto.IdempotencyKey, "duplicate", existing.Id, null, "Zaten senkronize edilmiş."),
                "Zaten kaydedilmiş.");
        }

        // 2. Sale kaydı oluştur (status=Completed)
        var saleId = Guid.NewGuid();
        var sale = new Sale
        {
            Id = saleId,
            IdempotencyKey = dto.IdempotencyKey,
            BranchOfficeId = dto.BranchOfficeId,
            SalePersonId = dto.SalePersonId,
            CustomerId = dto.CustomerId,
            GeneralDiscount = dto.GeneralDiscount,
            SaleDate = DateTimeOffset.UtcNow,
            OccurredAt = dto.OccurredAt,
            SaleSource = SaleSource.OfflinePos,
            SaleStatus = SaleStatus.Completed,
            SaleNumber = $"OFL-{dto.IdempotencyKey[..Math.Min(8, dto.IdempotencyKey.Length)]}"
        };
        foreach (var item in dto.Items)
        {
            sale.SaleItems.Add(new SaleItem
            {
                Id = Guid.NewGuid(),
                ProductVariantId = item.ProductVariantId,
                BranchOfficeId = dto.BranchOfficeId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent
            });
        }
        dbContext.Sales.Add(sale);

        // 3. Stok decrement + negatif tespit
        // BranchOfficeStock yapısı: CurrentStock = FirstTotalStock - SoldQuantity (computed).
        // Decrement = SoldQuantity'yi artır.
        int? firstIncidentId = null;
        var hasOversell = false;
        foreach (var item in dto.Items)
        {
            var stock = await dbContext.BranchOfficeStocks
                .AsTracking()
                .FirstOrDefaultAsync(s => s.BranchOfficeId == dto.BranchOfficeId && s.ProductVariantId == item.ProductVariantId);

            if (stock is null)
            {
                // Stok kaydı yoksa 0 başlangıçla yarat — sonraki sale incrementinde negatife düşer.
                stock = new BranchOfficeStock
                {
                    BranchOfficeId = dto.BranchOfficeId,
                    ProductVariantId = item.ProductVariantId,
                    FirstTotalStock = 0,
                    SoldQuantity = 0
                };
                dbContext.BranchOfficeStocks.Add(stock);
            }

            var stockBefore = stock.FirstTotalStock - stock.SoldQuantity;
            stock.SoldQuantity += item.Quantity;
            var stockAfter = stock.FirstTotalStock - stock.SoldQuantity;

            // StockMovement (audit trail)
            dbContext.Set<StockMovement>().Add(new StockMovement
            {
                BranchOfficeId = dto.BranchOfficeId,
                ProductVariantId = item.ProductVariantId,
                Type = StockMovementType.Sale,
                Quantity = -item.Quantity,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                ReferenceType = "OfflineSale",
                ReferenceId = saleId.ToString(),
                Note = $"OfflinePos sync: {dto.IdempotencyKey}"
            });

            if (stockAfter < 0)
            {
                hasOversell = true;
                logger.LogWarning(
                    "Negatif stok algılandı: variant={Variant} branch={Branch} before={Before} after={After}",
                    item.ProductVariantId, dto.BranchOfficeId, stockBefore, stockAfter);

                var incidentResult = await incidentManager.CreateAsync(new CreateIncidentDto(
                    TenantId: tenantId,
                    BranchOfficeId: dto.BranchOfficeId,
                    ProductVariantId: item.ProductVariantId,
                    Quantity: item.Quantity,
                    StockBefore: stockBefore,
                    StockAfter: stockAfter,
                    TriggeringSource: "OfflinePos",
                    TriggeringReferenceId: dto.IdempotencyKey,
                    TriggeringSaleId: saleId));

                if (incidentResult.Success && firstIncidentId is null)
                    firstIncidentId = incidentResult.Data.Id;
            }
        }

        // 4. Atomic commit — sale + stock + movements + incidents
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Offline POS satış senkron edildi: {dto.IdempotencyKey} (oversell={hasOversell})",
            LogType.Sale,
            LogAction.Add);

        var status = hasOversell ? "oversold" : "ok";
        return new SuccessDataResult<OfflineSaleSyncResult>(
            new OfflineSaleSyncResult(dto.IdempotencyKey, status, saleId, firstIncidentId, null),
            "Senkronize edildi.");
    }
}
