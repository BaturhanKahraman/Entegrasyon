using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class SaleReturnManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IFluentValidator fluentValidator,
    IOfficeStockManager officeStockManager) : ISaleReturnManager
{
    public async Task<IResult> CreateReturnAsync(CreateSaleReturnDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await fluentValidator.ValidateAndThrowAsync(dto);

        var sale = await dbContext.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == dto.SaleId);

        if (sale is null)
            return new ErrorResult("Satış bulunamadı.");

        if (sale.SaleStatus == SaleStatus.Cancelled)
            return new ErrorResult("İptal edilmiş satış iade edilemez.");

        if (sale.SaleStatus == SaleStatus.FullReturn)
            return new ErrorResult("Bu satışın tamamı zaten iade edilmiş.");

        decimal totalRefund = 0;
        var returnItems = new List<SaleReturnItem>();

        foreach (var itemDto in dto.Items)
        {
            var saleItem = sale.SaleItems.FirstOrDefault(si => si.Id == itemDto.SaleItemId);
            if (saleItem is null)
                return new ErrorResult($"Satış kalemi bulunamadı: {itemDto.SaleItemId}");

            var availableQty = saleItem.Quantity - saleItem.ReturnedQuantity;
            if (itemDto.Quantity > availableQty)
                return new ErrorResult(
                    $"'{saleItem.ProductTitle}' için maksimum {availableQty} adet iade edilebilir.");

            // İade tutarı hesapla (KDV dahil)
            var unitPriceWithVat = saleItem.UnitPrice * (1 + (decimal)saleItem.TaxPercentage / 100m);
            totalRefund += Math.Round(unitPriceWithVat * itemDto.Quantity, 2);

            returnItems.Add(new SaleReturnItem
            {
                SaleItemId = itemDto.SaleItemId,
                Quantity = itemDto.Quantity,
                Reason = itemDto.Reason
            });

            // ReturnedQuantity güncelle
            saleItem.ReturnedQuantity += itemDto.Quantity;
        }

        var saleReturn = new SaleReturn
        {
            SaleId = dto.SaleId,
            ReturnDate = DateTimeOffset.UtcNow,
            ReturnedByUserId = dto.ReturnedByUserId,
            ReturnStatus = ReturnStatus.Pending,
            ReturnReason = dto.ReturnReason,
            RefundPaymentMethodId = dto.RefundPaymentMethodId,
            RefundAmount = totalRefund,
            Note = dto.Note,
            Items = returnItems
        };

        // Sale durumunu güncelle
        var allItemsFullyReturned = sale.SaleItems.All(si => si.ReturnedQuantity >= si.Quantity);
        sale.SaleStatus = allItemsFullyReturned ? SaleStatus.FullReturn : SaleStatus.PartialReturn;

        dbContext.SaleReturns.Add(saleReturn);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade talebi oluşturuldu: {sale.SaleNumber}, Tutar: {totalRefund:C}",
            LogType.Sale, LogAction.Add);

        return new SuccessResult("İade talebi oluşturuldu.");
    }

    public async Task<IResult> ApproveReturnAsync(long returnId, Guid approvedByUserId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem)
            .Include(r => r.Sale)
            .FirstOrDefaultAsync(r => r.Id == returnId);

        if (saleReturn is null)
            return new ErrorResult("İade kaydı bulunamadı.");

        if (saleReturn.ReturnStatus != ReturnStatus.Pending)
            return new ErrorResult("Bu iade zaten işlenmiş.");

        saleReturn.ReturnStatus = ReturnStatus.Approved;
        saleReturn.ApprovedByUserId = approvedByUserId;

        // Stok geri artır
        foreach (var item in saleReturn.Items)
        {
            var stockResult = await officeStockManager.IncreaseStockAtomicAsync(
                saleReturn.Sale.BranchOfficeId,
                item.SaleItem.ProductVariantId,
                item.Quantity,
                StockMovementType.Return,
                $"İade onayı: {saleReturn.Sale.SaleNumber}");

            if (!stockResult.Success)
                return new ErrorResult(stockResult.Message ?? "Stok güncellenemedi.");
        }

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade onaylandı: {saleReturn.Sale.SaleNumber}",
            LogType.Sale, LogAction.Update);

        return new SuccessResult("İade onaylandı, stok güncellendi.");
    }

    public async Task<IResult> RejectReturnAsync(long returnId, Guid rejectedByUserId, string reason)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem)
            .Include(r => r.Sale).ThenInclude(s => s.SaleItems)
            .FirstOrDefaultAsync(r => r.Id == returnId);

        if (saleReturn is null)
            return new ErrorResult("İade kaydı bulunamadı.");

        if (saleReturn.ReturnStatus != ReturnStatus.Pending)
            return new ErrorResult("Bu iade zaten işlenmiş.");

        saleReturn.ReturnStatus = ReturnStatus.Rejected;
        saleReturn.Note = (saleReturn.Note ?? "") + $" [Ret nedeni: {reason}]";

        // ReturnedQuantity geri al
        foreach (var item in saleReturn.Items)
            item.SaleItem.ReturnedQuantity -= item.Quantity;

        // Sale durumunu yeniden hesapla
        var sale = saleReturn.Sale;
        var hasAnyReturn = sale.SaleItems.Any(si => si.ReturnedQuantity > 0);
        var allReturned = sale.SaleItems.All(si => si.ReturnedQuantity >= si.Quantity);
        sale.SaleStatus = allReturned ? SaleStatus.FullReturn
            : hasAnyReturn ? SaleStatus.PartialReturn
            : SaleStatus.Completed;

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade reddedildi: {sale.SaleNumber}",
            LogType.Sale, LogAction.Update);

        return new SuccessResult("İade reddedildi.");
    }

    public async Task<IDataResult<SaleReturn>> GetReturnByIdAsync(long returnId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem)
            .Include(r => r.ReturnedBy)
            .Include(r => r.ApprovedBy)
            .Include(r => r.RefundPaymentMethod)
            .FirstOrDefaultAsync(r => r.Id == returnId);

        return saleReturn is null
            ? new ErrorDataResult<SaleReturn>(null!, "İade kaydı bulunamadı.")
            : new SuccessDataResult<SaleReturn>(saleReturn);
    }
}
