using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Constants;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Notifications;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Standalone stok transfer talep iş akışı.
///
/// Lifecycle: Pending → Approved | Rejected.
/// CreateAsync: validate + business rules + insert request + items + notify approvers
/// ApproveAsync: validate + business rules + map items → call OfficeStockManager.TransferStockAsync
///   - Success → Status=Approved, audit log per item (Türkçe template), notify requester
///   - Fail → talep Pending kalır (TransferStockAsync atomic, partial state imkansız)
/// RejectAsync: reason zorunlu, Status=Rejected, notify requester
/// </summary>
public class StockTransferRequestManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IOfficeStockManager officeStockManager,
    INotificationManager notificationManager,
    INotificationRecipientResolver recipientResolver,
    IValidator<CreateStockTransferRequestDto> createValidator,
    IValidator<RejectStockTransferRequestDto> rejectValidator,
    ILogger<StockTransferRequestManager> logger) : IStockTransferRequestManager
{
    // ─── CreateAsync ───────────────────────────────────────────────────────

    public async Task<IDataResult<int>> CreateAsync(
        int sourceId, int targetId,
        IReadOnlyList<TransferItemDto> items,
        Guid requestedBy,
        CancellationToken ct = default)
    {
        // 1. Validation
        var dto = new CreateStockTransferRequestDto(sourceId, targetId, items);
        var validationResult = await createValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            return new ErrorDataResult<int>(0,
                string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage)));

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        // 2. Business rules
        var sourceActive = await dbContext.BranchOffices
            .AnyAsync(b => b.Id == sourceId && !b.IsDeleted, ct);
        if (!sourceActive)
            return new ErrorDataResult<int>(0, "Kaynak şube aktif değil veya bulunamadı.");

        var targetActive = await dbContext.BranchOffices
            .AnyAsync(b => b.Id == targetId && !b.IsDeleted, ct);
        if (!targetActive)
            return new ErrorDataResult<int>(0, "Hedef şube aktif değil veya bulunamadı.");

        // Informational stock pre-check (plana göre BLOKLAMAZ — onay anında re-check yapılır)
        // Yetersiz stok varsa sadece warning log, talep Pending olarak açılır.
        foreach (var item in items)
        {
            var hasEnough = await dbContext.BranchOfficeStocks
                .AnyAsync(s => s.BranchOfficeId == sourceId
                               && s.ProductVariantId == item.ProductVariantId
                               && (s.FirstTotalStock - s.SoldQuantity) >= item.Quantity, ct);
            if (!hasEnough)
            {
                logger.LogWarning(
                    "Transfer request informational warning: insufficient stock for variant {VariantId} (requested {Qty}) at source branch {SourceId}. Request still allowed; will be re-validated at approval time.",
                    item.ProductVariantId, item.Quantity, sourceId);
            }
        }

        // 3. Execution
        await applicationLogManager.AddLog(
            "Stok transfer talebi açılıyor.",
            LogType.StockTransfer, LogAction.RequestOpen,
            "BranchOffice", sourceId.ToString(),
            new { sourceId, targetId, requestedBy, itemCount = items.Count }, ct);

        var request = new StockTransferRequest
        {
            SourceBranchOfficeId = sourceId,
            TargetBranchOfficeId = targetId,
            RequestedByUserId = requestedBy,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = StockTransferRequestStatus.Pending,
            Items = items.Select(i => new StockTransferRequestItem
            {
                ProductVariantId = i.ProductVariantId,
                Quantity = i.Quantity
            }).ToList()
        };
        dbContext.StockTransferRequests.Add(request);
        await dbContext.SaveChangesAsync(ct);

        await applicationLogManager.AddLog(
            $"Stok transfer talebi #{request.Id} açıldı.",
            LogType.StockTransfer, LogAction.RequestOpen,
            "BranchOffice", sourceId.ToString(), null, ct);

        // Notify approvers
        try
        {
            var recipients = await recipientResolver.ResolveByPermissionAsync(
                PermissionConstants.StockTransferApprove, ct);
            if (recipients.Count > 0)
            {
                var sourceName = await dbContext.BranchOffices.AsNoTracking()
                    .Where(b => b.Id == sourceId).Select(b => b.Name ?? "").FirstOrDefaultAsync(ct);
                var targetName = await dbContext.BranchOffices.AsNoTracking()
                    .Where(b => b.Id == targetId).Select(b => b.Name ?? "").FirstOrDefaultAsync(ct);
                await notificationManager.SendNotification(
                    "Yeni stok transfer talebi",
                    $"{sourceName} → {targetName} arası stok transfer talebi açıldı.",
                    NotificationSeverity.Info,
                    NotificationCategory.Stok,
                    recipients,
                    actionUrl: $"/stock-transfer-requests/{request.Id}");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stock transfer create notification dispatch failed (non-critical)");
        }

        return new SuccessDataResult<int>(request.Id, "Stok transfer talebi oluşturuldu.");
    }

    // ─── ApproveAsync ──────────────────────────────────────────────────────

    public async Task<IResult> ApproveAsync(int requestId, Guid approvedBy, CancellationToken ct = default)
    {
        if (requestId <= 0)
            return new ErrorResult("Talep kimliği geçersiz.");

        // 1. Fetch + business rules
        StockTransferRequest? request;
        await using (var dbContext = await contextFactory.CreateDbContextAsync(ct))
        {
            request = await dbContext.StockTransferRequests
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);
            if (request is null)
                return new ErrorResult("Stok transfer talebi bulunamadı.");
            if (request.Status != StockTransferRequestStatus.Pending)
                return new ErrorResult("Bu talep zaten sonuçlandırılmış.");

            // Target hâlâ aktif mi?
            var targetActive = await dbContext.BranchOffices
                .AnyAsync(b => b.Id == request.TargetBranchOfficeId && !b.IsDeleted, ct);
            if (!targetActive)
                return new ErrorResult("Hedef şube aktif değil, talep onaylanamıyor.");

            var sourceActive = await dbContext.BranchOffices
                .AnyAsync(b => b.Id == request.SourceBranchOfficeId && !b.IsDeleted, ct);
            if (!sourceActive)
                return new ErrorResult("Kaynak şube aktif değil, talep onaylanamıyor.");
        }

        // 2. Mapping: StockTransferRequestItem → TransferItemDto
        var transferItems = request.Items
            .Select(i => new TransferItemDto(i.ProductVariantId, i.Quantity))
            .ToList();

        // 3. Execute transfer (atomic — TransferStockAsync stok yetersizliğini de kontrol eder)
        var transferResult = await officeStockManager.TransferStockAsync(
            request.SourceBranchOfficeId,
            request.TargetBranchOfficeId,
            transferItems);

        if (!transferResult.Success)
        {
            // Talep Pending kalır — kullanıcı stok düzeltip yeniden onay deneyebilir
            logger.LogWarning("Transfer execution failed for request {RequestId}: {Message}",
                requestId, transferResult.Message);
            return new ErrorResult($"Transfer başarısız: {transferResult.Message}");
        }

        // 4. Mark as approved
        await using (var dbContext = await contextFactory.CreateDbContextAsync(ct))
        {
            var tracked = await dbContext.StockTransferRequests.AsTracking()
                .FirstAsync(r => r.Id == requestId, ct);
            tracked.Status = StockTransferRequestStatus.Approved;
            tracked.ApprovedByUserId = approvedBy;
            tracked.ReviewedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }

        // 5. Audit log per item (Türkçe template) — Faz 4 ile aynı format
        string approverName, sourceName, targetName;
        await using (var dbContext = await contextFactory.CreateDbContextAsync(ct))
        {
            approverName = await dbContext.Users.AsNoTracking()
                .Where(u => u.Id == approvedBy)
                .Select(u => u.FullName ?? u.UserName ?? "")
                .FirstOrDefaultAsync(ct) ?? "";
            sourceName = await dbContext.BranchOffices.AsNoTracking()
                .Where(b => b.Id == request.SourceBranchOfficeId)
                .Select(b => b.Name ?? "")
                .FirstOrDefaultAsync(ct) ?? "";
            targetName = await dbContext.BranchOffices.AsNoTracking()
                .Where(b => b.Id == request.TargetBranchOfficeId)
                .Select(b => b.Name ?? "")
                .FirstOrDefaultAsync(ct) ?? "";

            var variantIds = transferItems.Select(i => i.ProductVariantId).ToList();
            var titles = await dbContext.ProductVariants.AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .Select(v => new { v.Id, Title = v.Product.Title ?? "" })
                .ToDictionaryAsync(x => x.Id, x => x.Title, ct);

            foreach (var item in transferItems)
            {
                var title = titles.TryGetValue(item.ProductVariantId, out var t) ? t : "";
                await applicationLogManager.AddLog(
                    $"{item.Quantity} adet {title} {sourceName}'ten {targetName}'e aktarıldı, onaylayan: {approverName}",
                    LogType.StockTransfer, LogAction.Transfer,
                    "BranchOffice", request.SourceBranchOfficeId.ToString(), null, ct);
            }
        }

        // 6. Notify requester
        try
        {
            await notificationManager.SendNotification(
                "Transfer talebiniz onaylandı",
                $"{sourceName} → {targetName} stok transferi gerçekleştirildi.",
                NotificationSeverity.Info,
                NotificationCategory.Stok,
                [request.RequestedByUserId],
                actionUrl: $"/stock-transfer-requests/{requestId}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Approve notification dispatch failed (non-critical)");
        }

        return new SuccessResult("Stok transferi başarıyla gerçekleştirildi.");
    }

    // ─── RejectAsync ───────────────────────────────────────────────────────

    public async Task<IResult> RejectAsync(int requestId, Guid rejectedBy, string reason, CancellationToken ct = default)
    {
        var dto = new RejectStockTransferRequestDto(requestId, reason);
        var validationResult = await rejectValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            return new ErrorResult(string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage)));

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var request = await dbContext.StockTransferRequests.AsTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null)
            return new ErrorResult("Stok transfer talebi bulunamadı.");
        if (request.Status != StockTransferRequestStatus.Pending)
            return new ErrorResult("Bu talep zaten sonuçlandırılmış.");

        request.Status = StockTransferRequestStatus.Rejected;
        request.ApprovedByUserId = rejectedBy;
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.RejectionReason = reason;
        await dbContext.SaveChangesAsync(ct);

        await applicationLogManager.AddLog(
            $"Stok transfer talebi #{requestId} reddedildi. Gerekçe: {reason}",
            LogType.StockTransfer, LogAction.RequestReject,
            "BranchOffice", request.SourceBranchOfficeId.ToString(),
            new { requestId, rejectedBy, reason }, ct);

        try
        {
            await notificationManager.SendNotification(
                "Transfer talebiniz reddedildi",
                $"Stok transfer talebiniz reddedildi. Gerekçe: {reason}",
                NotificationSeverity.Warning,
                NotificationCategory.Stok,
                [request.RequestedByUserId],
                actionUrl: $"/stock-transfer-requests/{requestId}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reject notification dispatch failed (non-critical)");
        }

        return new SuccessResult("Stok transfer talebi reddedildi.");
    }

    // ─── Queries ───────────────────────────────────────────────────────────

    public async Task<IDataResult<Pageable<StockTransferRequestListItemDto>>> GetPagedAsync(
        int pageIndex = 0, int pageSize = 50,
        StockTransferRequestStatus? status = null,
        CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var query = dbContext.StockTransferRequests.AsNoTracking().AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(r => new StockTransferRequestListItemDto(
                r.Id,
                r.SourceBranchOfficeId,
                dbContext.BranchOffices.IgnoreQueryFilters()
                    .Where(b => b.Id == r.SourceBranchOfficeId).Select(b => b.Name ?? "").FirstOrDefault() ?? "",
                r.TargetBranchOfficeId,
                dbContext.BranchOffices.IgnoreQueryFilters()
                    .Where(b => b.Id == r.TargetBranchOfficeId).Select(b => b.Name ?? "").FirstOrDefault() ?? "",
                (int)r.Status,
                StatusText(r.Status),
                r.RequestedByUser.FullName ?? r.RequestedByUser.UserName ?? "",
                r.RequestedAt,
                r.Items.Count,
                r.Items.Sum(i => i.Quantity),
                r.ReviewedAt,
                r.RejectionReason))
            .ToListAsync(ct);

        return new SuccessDataResult<Pageable<StockTransferRequestListItemDto>>(
            new Pageable<StockTransferRequestListItemDto>(items, pageIndex, pageSize, total));
    }

    public async Task<IDataResult<StockTransferRequestDetailDto>> GetByIdAsync(
        int requestId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var r = await dbContext.StockTransferRequests.AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == requestId, ct);

        if (r is null)
            return new ErrorDataResult<StockTransferRequestDetailDto>(null!, "Talep bulunamadı.");

        var sourceName = await dbContext.BranchOffices.IgnoreQueryFilters()
            .Where(b => b.Id == r.SourceBranchOfficeId).Select(b => b.Name ?? "").FirstOrDefaultAsync(ct) ?? "";
        var targetName = await dbContext.BranchOffices.IgnoreQueryFilters()
            .Where(b => b.Id == r.TargetBranchOfficeId).Select(b => b.Name ?? "").FirstOrDefaultAsync(ct) ?? "";

        // Item title + canlı kaynak stok
        var variantIds = r.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var variantRaw = await dbContext.ProductVariants.AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .Select(v => new
            {
                v.Id,
                Title = v.Product.Title ?? "",
                Barcode = v.Barcode ?? "",
                v.Name,
                RawAttrs = v.ProductVariantAttributes.Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer }).ToList()
            })
            .ToListAsync(ct);

        var variantInfo = variantRaw.ToDictionary(x => x.Id, x => (
            x.Title,
            x.Barcode,
            DisplayName: VariantNameExtensions.ResolveDisplayName(
                x.Name,
                x.RawAttrs.Select((a, i) => new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
                x.Title)
        ));

        var liveStocks = await dbContext.BranchOfficeStocks.AsNoTracking()
            .Where(s => s.BranchOfficeId == r.SourceBranchOfficeId
                        && s.ProductVariantId.HasValue
                        && variantIds.Contains(s.ProductVariantId.Value))
            .Select(s => new { VariantId = s.ProductVariantId!.Value, Stock = s.FirstTotalStock - s.SoldQuantity })
            .ToDictionaryAsync(x => x.VariantId, x => x.Stock, ct);

        var itemDtos = r.Items
            .Select(i =>
            {
                variantInfo.TryGetValue(i.ProductVariantId, out var info);
                return new StockTransferItemDetailDto(
                    i.ProductVariantId,
                    info.Title ?? "",
                    info.DisplayName ?? "",
                    info.Barcode ?? "",
                    i.Quantity,
                    liveStocks.TryGetValue(i.ProductVariantId, out var stock) ? stock : 0);
            })
            .ToList();

        var detail = new StockTransferRequestDetailDto(
            r.Id,
            r.SourceBranchOfficeId,
            sourceName,
            r.TargetBranchOfficeId,
            targetName,
            (int)r.Status,
            StatusText(r.Status),
            r.RequestedByUserId,
            r.RequestedByUser.FullName ?? r.RequestedByUser.UserName ?? "",
            r.RequestedAt,
            r.ApprovedByUserId,
            r.ApprovedByUser?.FullName ?? r.ApprovedByUser?.UserName,
            r.ReviewedAt,
            r.RejectionReason,
            itemDtos);

        return new SuccessDataResult<StockTransferRequestDetailDto>(detail);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        return await dbContext.StockTransferRequests
            .CountAsync(r => r.Status == StockTransferRequestStatus.Pending, ct);
    }

    private static string StatusText(StockTransferRequestStatus status) => status switch
    {
        StockTransferRequestStatus.Pending => "Beklemede",
        StockTransferRequestStatus.Approved => "Onaylandı",
        StockTransferRequestStatus.Rejected => "Reddedildi",
        _ => "-"
    };
}
