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
    // ────────────────────────────────────────────────────────────────────
    // CREATE
    // ────────────────────────────────────────────────────────────────────

    public async Task<IDataResult<long>> CreateReturnAsync(CreateSaleReturnDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await fluentValidator.ValidateAndThrowAsync(dto);

        decimal totalRefund = 0;
        var returnItems = new List<SaleReturnItem>();

        if (dto.SaleId.HasValue)
        {
            var result = await BuildSaleReturnItems(dbContext, dto.SaleId.Value, dto.Items);
            if (!result.Success) return new ErrorDataResult<long>(0, result.Message ?? "İade oluşturulamadı.");
            totalRefund = result.Data.TotalRefund;
            returnItems = result.Data.Items;
        }
        else if (dto.OrderId.HasValue)
        {
            var result = await BuildOrderReturnItems(dbContext, dto.OrderId.Value, dto.Items);
            if (!result.Success) return new ErrorDataResult<long>(0, result.Message ?? "İade oluşturulamadı.");
            totalRefund = result.Data.TotalRefund;
            returnItems = result.Data.Items;
        }

        var saleReturn = new SaleReturn
        {
            SaleId = dto.SaleId,
            OrderId = dto.OrderId,
            Source = dto.Source,
            ReturnDate = DateTimeOffset.UtcNow,
            ReturnedByUserId = dto.ReturnedByUserId,
            ReturnStatus = dto.SubmitImmediately ? ReturnStatus.Pending : ReturnStatus.Draft,
            ReturnReasonId = dto.ReturnReasonId,
            CustomReason = dto.CustomReason,
            RefundPaymentMethodId = dto.RefundPaymentMethodId,
            RefundAmount = totalRefund,
            Note = dto.Note,
            Items = returnItems
        };

        dbContext.SaleReturns.Add(saleReturn);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade talebi oluşturuldu. Tutar: {totalRefund:C}",
            LogType.Sale, LogAction.Add);

        return new SuccessDataResult<long>(saleReturn.Id, "İade talebi oluşturuldu.");
    }

    // ────────────────────────────────────────────────────────────────────
    // UPDATE (Draft/Pending only)
    // ────────────────────────────────────────────────────────────────────

    public async Task<IResult> UpdateReturnAsync(UpdateSaleReturnDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await fluentValidator.ValidateAndThrowAsync(dto);

        var saleReturn = await dbContext.SaleReturns
            .AsTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == dto.Id);

        if (saleReturn is null)
            return new ErrorResult("İade kaydı bulunamadı.");

        if (saleReturn.ReturnStatus is not (ReturnStatus.Draft or ReturnStatus.Pending))
            return new ErrorResult("Sadece taslak veya bekleyen iadeler düzenlenebilir.");

        // Eski ReturnedQuantity geri al (Sale bazlı iadelerde)
        if (saleReturn.SaleId.HasValue)
            await RollbackReturnedQuantities(dbContext, saleReturn);

        // Eski item'ları sil
        dbContext.SaleReturnItems.RemoveRange(saleReturn.Items);

        // Yeni item'ları oluştur
        decimal totalRefund = 0;
        var newItems = new List<SaleReturnItem>();

        if (saleReturn.SaleId.HasValue)
        {
            var result = await BuildSaleReturnItems(dbContext, saleReturn.SaleId.Value, dto.Items);
            if (!result.Success) return result;
            totalRefund = result.Data.TotalRefund;
            newItems = result.Data.Items;
        }
        else if (saleReturn.OrderId.HasValue)
        {
            var result = await BuildOrderReturnItems(dbContext, saleReturn.OrderId.Value, dto.Items);
            if (!result.Success) return result;
            totalRefund = result.Data.TotalRefund;
            newItems = result.Data.Items;
        }

        saleReturn.Source = dto.Source;
        saleReturn.ReturnReasonId = dto.ReturnReasonId;
        saleReturn.CustomReason = dto.CustomReason;
        saleReturn.RefundPaymentMethodId = dto.RefundPaymentMethodId;
        saleReturn.RefundAmount = totalRefund;
        saleReturn.Note = dto.Note;
        saleReturn.Items = newItems;

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade düzenlendi: #{saleReturn.Id}",
            LogType.Sale, LogAction.Update);

        return new SuccessResult("İade güncellendi.");
    }

    // ────────────────────────────────────────────────────────────────────
    // SUBMIT (Draft → Pending)
    // ────────────────────────────────────────────────────────────────────

    public async Task<IResult> SubmitReturnAsync(long returnId, Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns.AsTracking().FirstOrDefaultAsync(r => r.Id == returnId);
        if (saleReturn is null) return new ErrorResult("İade kaydı bulunamadı.");
        if (saleReturn.ReturnStatus != ReturnStatus.Draft)
            return new ErrorResult("Sadece taslak durumundaki iadeler onaya sunulabilir.");

        saleReturn.ReturnStatus = ReturnStatus.Pending;
        await dbContext.SaveChangesAsync();

        return new SuccessResult("İade onaya sunuldu.");
    }

    // ────────────────────────────────────────────────────────────────────
    // APPROVE (artık stoğa dokunmaz — sadece status değişir)
    // ────────────────────────────────────────────────────────────────────

    public async Task<IResult> ApproveReturnAsync(long returnId, Guid approvedByUserId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns.AsTracking().FirstOrDefaultAsync(r => r.Id == returnId);
        if (saleReturn is null) return new ErrorResult("İade kaydı bulunamadı.");
        if (saleReturn.ReturnStatus != ReturnStatus.Pending)
            return new ErrorResult("Sadece bekleyen iadeler onaylanabilir.");

        saleReturn.ReturnStatus = ReturnStatus.Approved;
        saleReturn.ApprovedByUserId = approvedByUserId;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade onaylandı: #{saleReturn.Id}",
            LogType.Sale, LogAction.Update);

        return new SuccessResult("İade onaylandı.");
    }

    // ────────────────────────────────────────────────────────────────────
    // REJECT
    // ────────────────────────────────────────────────────────────────────

    public async Task<IResult> RejectReturnAsync(long returnId, Guid rejectedByUserId, string reason)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns
            .AsTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == returnId);

        if (saleReturn is null) return new ErrorResult("İade kaydı bulunamadı.");
        if (saleReturn.ReturnStatus != ReturnStatus.Pending)
            return new ErrorResult("Sadece bekleyen iadeler reddedilebilir.");

        saleReturn.ReturnStatus = ReturnStatus.Rejected;
        saleReturn.Note = (saleReturn.Note ?? "") + $" [Ret nedeni: {reason}]";

        // ReturnedQuantity geri al
        if (saleReturn.SaleId.HasValue)
            await RollbackReturnedQuantities(dbContext, saleReturn);

        // SaleStatus recalc
        if (saleReturn.SaleId.HasValue)
            await RecalculateSaleStatus(dbContext, saleReturn.SaleId.Value);

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade reddedildi: #{saleReturn.Id}",
            LogType.Sale, LogAction.Update);

        return new SuccessResult("İade reddedildi.");
    }

    // ────────────────────────────────────────────────────────────────────
    // CANCEL (Pending/Approved → Cancelled)
    // ────────────────────────────────────────────────────────────────────

    public async Task<IResult> CancelReturnAsync(CancelSaleReturnDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await fluentValidator.ValidateAndThrowAsync(dto);

        var saleReturn = await dbContext.SaleReturns
            .AsTracking()
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem)
            .Include(r => r.Items).ThenInclude(ri => ri.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == dto.ReturnId);

        if (saleReturn is null) return new ErrorResult("İade kaydı bulunamadı.");
        if (saleReturn.ReturnStatus is not (ReturnStatus.Pending or ReturnStatus.Approved))
            return new ErrorResult("Sadece bekleyen veya onaylanmış iadeler iptal edilebilir.");

        // Daha önce stok eklenmişse geri al
        foreach (var item in saleReturn.Items.Where(i => i.RestoredToStock))
        {
            var variantId = GetProductVariantId(item);
            if (variantId is null) continue;

            var branchOfficeId = saleReturn.RestoreBranchOfficeId ?? 1;
            var stockResult = await officeStockManager.DecreaseStockAtomicAsync(
                branchOfficeId, variantId.Value, item.Quantity,
                StockMovementType.Return, "SaleReturn", saleReturn.Id.ToString());

            if (!stockResult.Success)
                return new ErrorResult($"Stok geri alınamadı: {stockResult.Message}");

            item.RestoredToStock = false;
            item.RestoredAt = null;
            item.RestoredByUserId = null;
        }

        // ReturnedQuantity geri al
        if (saleReturn.SaleId.HasValue)
            await RollbackReturnedQuantities(dbContext, saleReturn);

        saleReturn.ReturnStatus = ReturnStatus.Cancelled;
        saleReturn.CancelledAt = DateTimeOffset.UtcNow;
        saleReturn.CancelledByUserId = dto.CancelledByUserId;
        saleReturn.CancellationReason = dto.Reason;

        if (saleReturn.SaleId.HasValue)
            await RecalculateSaleStatus(dbContext, saleReturn.SaleId.Value);

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade iptal edildi: #{saleReturn.Id}",
            LogType.Sale, LogAction.Delete);

        return new SuccessResult("İade iptal edildi.");
    }

    // ────────────────────────────────────────────────────────────────────
    // COMPLETE (Approved → Completed, selective stock restore)
    // ────────────────────────────────────────────────────────────────────

    public async Task<IResult> CompleteReturnAsync(CompleteSaleReturnDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await fluentValidator.ValidateAndThrowAsync(dto);

        var saleReturn = await dbContext.SaleReturns
            .AsTracking()
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem)
            .Include(r => r.Items).ThenInclude(ri => ri.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == dto.ReturnId);

        if (saleReturn is null) return new ErrorResult("İade kaydı bulunamadı.");
        if (saleReturn.ReturnStatus != ReturnStatus.Approved)
            return new ErrorResult("Sadece onaylanmış iadeler tamamlanabilir.");

        saleReturn.RestoreBranchOfficeId = dto.BranchOfficeId;

        // Seçili item'lar için stok ekle
        foreach (var item in saleReturn.Items.Where(i => dto.ItemIdsToRestore.Contains(i.Id) && !i.RestoredToStock))
        {
            var variantId = GetProductVariantId(item);
            if (variantId is null) continue;

            var stockResult = await officeStockManager.IncreaseStockAtomicAsync(
                dto.BranchOfficeId, variantId.Value, item.Quantity,
                StockMovementType.Return, "SaleReturn", saleReturn.Id.ToString());

            if (!stockResult.Success)
                return new ErrorResult($"Stok eklenemedi: {stockResult.Message}");

            item.RestoredToStock = true;
            item.RestoredAt = DateTimeOffset.UtcNow;
            item.RestoredByUserId = dto.CompletedByUserId;
        }

        saleReturn.ReturnStatus = ReturnStatus.Completed;
        saleReturn.CompletedAt = DateTimeOffset.UtcNow;
        saleReturn.CompletedByUserId = dto.CompletedByUserId;

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"İade tamamlandı: #{saleReturn.Id}",
            LogType.Sale, LogAction.Update);

        return new SuccessResult("İade tamamlandı.");
    }

    // ────────────────────────────────────────────────────────────────────
    // RESTORE SINGLE ITEM
    // ────────────────────────────────────────────────────────────────────

    public async Task<IResult> RestoreItemToStockAsync(long saleReturnItemId, Guid userId, int branchOfficeId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var item = await dbContext.SaleReturnItems
            .AsTracking()
            .Include(i => i.SaleReturn)
            .Include(i => i.SaleItem)
            .Include(i => i.OrderItem)
            .FirstOrDefaultAsync(i => i.Id == saleReturnItemId);

        if (item is null) return new ErrorResult("İade kalemi bulunamadı.");
        if (item.SaleReturn.ReturnStatus is not (ReturnStatus.Approved or ReturnStatus.Completed))
            return new ErrorResult("Sadece onaylanmış veya tamamlanmış iadelerin kalemleri envantere eklenebilir.");
        if (item.RestoredToStock)
            return new SuccessResult("Bu kalem zaten envantere eklenmiş.");

        var variantId = GetProductVariantId(item);
        if (variantId is null) return new ErrorResult("Ürün varyantı bulunamadı.");

        var stockResult = await officeStockManager.IncreaseStockAtomicAsync(
            branchOfficeId, variantId.Value, item.Quantity,
            StockMovementType.Return, "SaleReturn", item.SaleReturnId.ToString());

        if (!stockResult.Success)
            return new ErrorResult($"Stok eklenemedi: {stockResult.Message}");

        item.RestoredToStock = true;
        item.RestoredAt = DateTimeOffset.UtcNow;
        item.RestoredByUserId = userId;
        item.SaleReturn.RestoreBranchOfficeId ??= branchOfficeId;

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Ürün envantere eklendi.");
    }

    // ────────────────────────────────────────────────────────────────────
    // QUERY
    // ────────────────────────────────────────────────────────────────────

    public async Task<IDataResult<SaleReturn>> GetReturnByIdAsync(long returnId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var saleReturn = await dbContext.SaleReturns
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem!).ThenInclude(si => si.ProductVariant).ThenInclude(pv => pv.Product)
            .Include(r => r.Items).ThenInclude(ri => ri.SaleItem!).ThenInclude(si => si.ProductVariant).ThenInclude(pv => pv.ProductVariantAttributes)
            .Include(r => r.Items).ThenInclude(ri => ri.OrderItem)
            .Include(r => r.ReturnedBy)
            .Include(r => r.ApprovedBy)
            .Include(r => r.CompletedBy)
            .Include(r => r.CancelledBy)
            .Include(r => r.ReturnReason)
            .Include(r => r.RefundPaymentMethod)
            .Include(r => r.RestoreBranchOffice)
            .FirstOrDefaultAsync(r => r.Id == returnId);

        return saleReturn is null
            ? new ErrorDataResult<SaleReturn>(null!, "İade kaydı bulunamadı.")
            : new SuccessDataResult<SaleReturn>(saleReturn);
    }

    // ────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ────────────────────────────────────────────────────────────────────

    private static Guid? GetProductVariantId(SaleReturnItem item)
    {
        if (item.SaleItem is not null) return item.SaleItem.ProductVariantId;
        if (item.OrderItem?.ProductId is not null) return item.OrderItem.ProductId;
        return null;
    }

    private async Task<IDataResult<(decimal TotalRefund, List<SaleReturnItem> Items)>>
        BuildSaleReturnItems(IntegrationDbContext dbContext, Guid saleId, List<SaleReturnItemDto> itemDtos)
    {
        var sale = await dbContext.Sales
            .AsTracking()
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null)
            return new ErrorDataResult<(decimal, List<SaleReturnItem>)>(default, "Satış bulunamadı.");

        if (sale.SaleStatus == SaleStatus.Cancelled)
            return new ErrorDataResult<(decimal, List<SaleReturnItem>)>(default, "İptal edilmiş satış iade edilemez.");

        if (sale.SaleStatus == SaleStatus.FullReturn)
            return new ErrorDataResult<(decimal, List<SaleReturnItem>)>(default, "Bu satışın tamamı zaten iade edilmiş.");

        decimal totalRefund = 0;
        var items = new List<SaleReturnItem>();

        foreach (var itemDto in itemDtos)
        {
            if (!itemDto.SaleItemId.HasValue) continue;

            var saleItem = sale.SaleItems.FirstOrDefault(si => si.Id == itemDto.SaleItemId.Value);
            if (saleItem is null)
                return new ErrorDataResult<(decimal, List<SaleReturnItem>)>(default, $"Satış kalemi bulunamadı: {itemDto.SaleItemId}");

            var availableQty = saleItem.Quantity - saleItem.ReturnedQuantity;
            if (itemDto.Quantity > availableQty)
                return new ErrorDataResult<(decimal, List<SaleReturnItem>)>(default,
                    $"'{saleItem.ProductTitle}' için maksimum {availableQty} adet iade edilebilir.");

            var unitPriceWithVat = saleItem.UnitPrice * (1 + (decimal)saleItem.TaxPercentage / 100m);
            totalRefund += Math.Round(unitPriceWithVat * itemDto.Quantity, 2);

            items.Add(new SaleReturnItem
            {
                SaleItemId = itemDto.SaleItemId,
                Quantity = itemDto.Quantity,
                Reason = itemDto.Reason
            });

            saleItem.ReturnedQuantity += itemDto.Quantity;
        }

        // SaleStatus güncelle
        var allReturned = sale.SaleItems.All(si => si.ReturnedQuantity >= si.Quantity);
        sale.SaleStatus = allReturned ? SaleStatus.FullReturn : SaleStatus.PartialReturn;

        return new SuccessDataResult<(decimal TotalRefund, List<SaleReturnItem> Items)>((totalRefund, items));
    }

    private async Task<IDataResult<(decimal TotalRefund, List<SaleReturnItem> Items)>>
        BuildOrderReturnItems(IntegrationDbContext dbContext, Guid orderId, List<SaleReturnItemDto> itemDtos)
    {
        var order = await dbContext.Orders
            .AsTracking()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new ErrorDataResult<(decimal, List<SaleReturnItem>)>(default, "Sipariş bulunamadı.");

        decimal totalRefund = 0;
        var items = new List<SaleReturnItem>();

        foreach (var itemDto in itemDtos)
        {
            if (!itemDto.OrderItemId.HasValue) continue;

            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.Id == itemDto.OrderItemId.Value);
            if (orderItem is null)
                return new ErrorDataResult<(decimal, List<SaleReturnItem>)>(default, $"Sipariş kalemi bulunamadı: {itemDto.OrderItemId}");

            var availableQty = orderItem.Quantity - orderItem.ReturnedQuantity;
            if (itemDto.Quantity > availableQty)
                return new ErrorDataResult<(decimal, List<SaleReturnItem>)>(default,
                    $"Bu kalem için maksimum {availableQty} adet iade edilebilir.");

            totalRefund += Math.Round(orderItem.UnitPrice * itemDto.Quantity, 2);

            items.Add(new SaleReturnItem
            {
                OrderItemId = itemDto.OrderItemId,
                Quantity = itemDto.Quantity,
                Reason = itemDto.Reason
            });

            orderItem.ReturnedQuantity += itemDto.Quantity;
        }

        return new SuccessDataResult<(decimal TotalRefund, List<SaleReturnItem> Items)>((totalRefund, items));
    }

    private async Task RollbackReturnedQuantities(IntegrationDbContext dbContext, SaleReturn saleReturn)
    {
        if (!saleReturn.SaleId.HasValue) return;

        var sale = await dbContext.Sales
            .AsTracking()
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == saleReturn.SaleId.Value);

        if (sale is null) return;

        foreach (var item in saleReturn.Items.Where(i => i.SaleItemId.HasValue))
        {
            var saleItem = sale.SaleItems.FirstOrDefault(si => si.Id == item.SaleItemId!.Value);
            if (saleItem is not null)
                saleItem.ReturnedQuantity = Math.Max(0, saleItem.ReturnedQuantity - item.Quantity);
        }
    }

    private async Task RecalculateSaleStatus(IntegrationDbContext dbContext, Guid saleId)
    {
        var sale = await dbContext.Sales
            .AsTracking()
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale is null) return;

        var hasAnyReturn = sale.SaleItems.Any(si => si.ReturnedQuantity > 0);
        var allReturned = sale.SaleItems.All(si => si.ReturnedQuantity >= si.Quantity);
        sale.SaleStatus = allReturned ? SaleStatus.FullReturn
            : hasAnyReturn ? SaleStatus.PartialReturn
            : SaleStatus.Completed;
    }
}
