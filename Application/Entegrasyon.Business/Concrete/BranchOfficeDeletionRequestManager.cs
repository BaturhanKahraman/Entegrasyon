using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.System;
using Entegrasyon.Business.Constants;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Utilities;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.POS;
using Entegrasyon.Entity.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Şube ofisi silme talep iş akışı.
///
/// Approve akışı iki stage:
///
/// Stage A — Transfer (lock DIŞINDA, idempotent, bounded retry):
///   Loop max 3x:
///     live items = source'daki > 0 stok
///     if boş → break
///     TransferStockAsync(source, target, items)
///   remaining > 0 ise hata dön (eşzamanlı değişiklikler)
///
/// Stage B — Soft-delete + reassign (advisory lock altında, tek transaction):
///   pg_advisory_xact_lock ile serialized
///   activeCount kontrolü (son aktif ofis race'i)
///   branch.IsDeleted = true, DeletionRequestId = null
///   DefaultBranchOfficeId reassign → HQ
///   UserBranchOffices junction'ı sil
///   LastSelectedBranchOfficeId temizle
///   request.Status = Approved
///
/// Stage B fail olursa Stage A zaten bitmiş → bir sonraki approve denemesi Stage A'yı atlar (boş),
/// sadece Stage B'yi tekrar çalıştırır. İdempotent doğal recovery.
/// </summary>
public class BranchOfficeDeletionRequestManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    IOfficeStockManager officeStockManager,
    INotificationManager notificationManager,
    INotificationRecipientResolver recipientResolver,
    IValidator<RequestDeleteDto> requestValidator,
    IValidator<RejectDeletionRequestDto> rejectValidator,
    IOptions<NotificationFeatureFlags> notificationFlags,
    ILogger<BranchOfficeDeletionRequestManager> logger) : IBranchOfficeDeletionRequestManager
{
    private const int MaxStageARetries = 3;
    private const string AdvisoryLockName = "branch_office_deletion_approval";

    // ─── RequestDeleteAsync ────────────────────────────────────────────────

    public async Task<IDataResult<int>> RequestDeleteAsync(
        int branchOfficeId, Guid requestedBy, int? targetBranchOfficeId, CancellationToken ct = default)
    {
        // 1. Validation
        var dto = new RequestDeleteDto(branchOfficeId, targetBranchOfficeId);
        var validationResult = await requestValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            return new ErrorDataResult<int>(0,
                string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage)));

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        // 2. Business rules (LogicRunner)
        var rule = LogicRunner.Run(
            await CheckOfficeExistsAndActiveAsync(dbContext, branchOfficeId, ct),
            await CheckNotHQAsync(dbContext, branchOfficeId, ct),
            await CheckNoPendingRequestAsync(dbContext, branchOfficeId, ct),
            await CheckNoActivePosSessionAsync(dbContext, branchOfficeId, ct),
            await CheckNotDefaultMarketplaceStockAsync(dbContext, branchOfficeId, ct),
            await CheckAtLeastOneActiveBranchRemainsAsync(dbContext, branchOfficeId, ct));
        if (rule != null)
            return new ErrorDataResult<int>(0, rule.Message!);

        // Stok kontrolü: source'ta stok varsa target zorunlu + target aktif olmalı
        var hasStock = await dbContext.BranchOfficeStocks
            .AnyAsync(s => s.BranchOfficeId == branchOfficeId && (s.FirstTotalStock - s.SoldQuantity) > 0, ct);

        if (hasStock)
        {
            if (!targetBranchOfficeId.HasValue)
                return new ErrorDataResult<int>(0,
                    "Silinecek şubede stok var. Lütfen stoğun aktarılacağı hedef şubeyi seçin.");

            var targetExists = await dbContext.BranchOffices
                .AnyAsync(b => b.Id == targetBranchOfficeId.Value && !b.IsDeleted, ct);
            if (!targetExists)
                return new ErrorDataResult<int>(0, "Hedef şube aktif değil veya bulunamadı.");
        }

        // 3. Execution
        await applicationLogManager.AddLog(
            "Şube silme talebi açılıyor.",
            LogType.BranchDeletion, LogAction.RequestOpen,
            "BranchOffice", branchOfficeId.ToString(),
            new { branchOfficeId, requestedBy, targetBranchOfficeId }, ct);

        // Snapshot items — talep anındaki stok (UI/audit için, transactional değil)
        var snapshotItems = await dbContext.BranchOfficeStocks
            .Where(s => s.BranchOfficeId == branchOfficeId
                        && s.ProductVariantId.HasValue
                        && (s.FirstTotalStock - s.SoldQuantity) > 0)
            .Select(s => new BranchOfficeDeletionRequestItem
            {
                ProductVariantId = s.ProductVariantId!.Value,
                Quantity = s.FirstTotalStock - s.SoldQuantity
            })
            .ToListAsync(ct);

        var request = new BranchOfficeDeletionRequest
        {
            BranchOfficeId = branchOfficeId,
            RequestedByUserId = requestedBy,
            RequestedAt = DateTimeOffset.UtcNow,
            Status = BranchOfficeDeletionRequestStatus.Pending,
            TransferTargetBranchOfficeId = hasStock ? targetBranchOfficeId : null,
            HasStockTransfer = hasStock,
            Items = snapshotItems
        };
        dbContext.BranchOfficeDeletionRequests.Add(request);
        await dbContext.SaveChangesAsync(ct);

        // Denormalized cache: branch.DeletionRequestId = request.Id
        await dbContext.BranchOffices
            .Where(b => b.Id == branchOfficeId)
            .ExecuteUpdateAsync(b => b.SetProperty(x => x.DeletionRequestId, request.Id), ct);

        if (notificationFlags.Value.PublishEnabled)
        {
            dbContext.AddDomainEvent(new BranchOfficeApprovalRequestedEvent(
                request.Id,
                request.BranchOfficeId,
                requestedBy));
            await dbContext.SaveChangesAsync(ct);
        }

        await applicationLogManager.AddLog(
            "Şube silme talebi açıldı.",
            LogType.BranchDeletion, LogAction.RequestOpen,
            "BranchOffice", branchOfficeId.ToString(), null, ct);

        // Notify approvers
        try
        {
            var recipients = await recipientResolver.ResolveByPermissionAsync(
                PermissionConstants.StockOfficeDeleteApprove, ct);
            if (recipients.Count > 0)
            {
                var branch = await dbContext.BranchOffices.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == branchOfficeId, ct);
                await notificationManager.SendNotification(
                    "Şube silme talebi",
                    $"{branch?.Name ?? "Şube"} için silme talebi açıldı.",
                    NotificationSeverity.Warning,
                    NotificationCategory.Sistem,
                    recipients,
                    actionUrl: $"/branch-office-deletion-requests/{request.Id}");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deletion request notification dispatch failed (non-critical)");
        }

        return new SuccessDataResult<int>(request.Id, "Silme talebi açıldı.");
    }

    // ─── ApproveAsync — Stage A + Stage B ──────────────────────────────────

    public async Task<IResult> ApproveAsync(int requestId, Guid approvedBy, CancellationToken ct = default)
    {
        // 1. Validation (minimal)
        if (requestId <= 0)
            return new ErrorResult("Talep kimliği geçersiz.");

        // 2. Business rules + request fetch
        BranchOfficeDeletionRequest request;
        await using (var dbContext = await contextFactory.CreateDbContextAsync(ct))
        {
            request = (await dbContext.BranchOfficeDeletionRequests
                .FirstOrDefaultAsync(r => r.Id == requestId, ct))!;
            if (request is null)
                return new ErrorResult("Silme talebi bulunamadı.");
            if (request.Status != BranchOfficeDeletionRequestStatus.Pending)
                return new ErrorResult("Bu talep zaten sonuçlandırılmış.");

            // Target aktif mi? (HasStockTransfer ise)
            if (request.HasStockTransfer && request.TransferTargetBranchOfficeId.HasValue)
            {
                var targetActive = await dbContext.BranchOffices
                    .AnyAsync(b => b.Id == request.TransferTargetBranchOfficeId.Value && !b.IsDeleted, ct);
                if (!targetActive)
                    return new ErrorResult("Hedef şube aktif değil, talep onaylanamıyor.");
            }
        }

        var sourceId = request.BranchOfficeId;
        var targetId = request.TransferTargetBranchOfficeId;

        // ── Stage A — Transfer (lock dışında, bounded retry) ───────────────
        var transferredItems = new List<(Guid VariantId, int Quantity)>();
        if (request.HasStockTransfer && targetId.HasValue)
        {
            for (var attempt = 1; attempt <= MaxStageARetries; attempt++)
            {
                List<TransferItemDto> liveItems;
                await using (var dbContext = await contextFactory.CreateDbContextAsync(ct))
                {
                    liveItems = await dbContext.BranchOfficeStocks
                        .Where(s => s.BranchOfficeId == sourceId
                                    && s.ProductVariantId.HasValue
                                    && (s.FirstTotalStock - s.SoldQuantity) > 0)
                        .Select(s => new TransferItemDto(
                            s.ProductVariantId!.Value,
                            s.FirstTotalStock - s.SoldQuantity))
                        .ToListAsync(ct);
                }

                if (liveItems.Count == 0)
                    break;

                var transferResult = await officeStockManager.TransferStockAsync(sourceId, targetId.Value, liveItems);
                if (!transferResult.Success)
                {
                    logger.LogError("Stage A transfer failed: {Message}", transferResult.Message);
                    return new ErrorResult($"Stok aktarımı başarısız: {transferResult.Message}");
                }

                foreach (var item in liveItems)
                    transferredItems.Add((item.ProductVariantId, item.Quantity));
            }

            // Son kontrol: source'ta hâlâ stok var mı?
            await using (var dbContext = await contextFactory.CreateDbContextAsync(ct))
            {
                var remaining = await dbContext.BranchOfficeStocks
                    .Where(s => s.BranchOfficeId == sourceId)
                    .SumAsync(s => (int?)(s.FirstTotalStock - s.SoldQuantity), ct) ?? 0;
                if (remaining > 0)
                    return new ErrorResult(
                        "Eşzamanlı stok değişiklikleri tamamlanamadı, lütfen tekrar deneyin.");
            }
        }

        // ── Stage B — Soft-delete + reassign (advisory lock) ───────────────
        int hqId;
        string branchName;
        await using (var ctx = await contextFactory.CreateDbContextAsync(ct))
        await using (var tx = await ctx.Database.BeginTransactionAsync(ct))
        {
            // CRITICAL: advisory lock — son aktif ofis race'i için serialized
            await ctx.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(hashtext({0}))",
                [AdvisoryLockName], ct);

            // Lock alındıktan sonra son kontrol
            var activeCountExcludingSource = await ctx.BranchOffices
                .CountAsync(b => !b.IsDeleted && b.Id != sourceId, ct);
            if (activeCountExcludingSource < 1)
            {
                await tx.RollbackAsync(ct);
                return new ErrorResult("Sistemde en az bir aktif şube kalmak zorunda.");
            }

            var branch = await ctx.BranchOffices.AsTracking()
                .FirstOrDefaultAsync(b => b.Id == sourceId, ct);
            if (branch is null)
            {
                await tx.RollbackAsync(ct);
                return new ErrorResult("Şube bulunamadı.");
            }

            branchName = branch.Name ?? $"Şube #{sourceId}";

            branch.IsDeleted = true;
            branch.DeletedAt = DateTimeOffset.UtcNow;
            branch.DeletionRequestId = null; // denormalized cache — soft-deleted'de null olmalı

            hqId = await ctx.BranchOffices
                .Where(b => b.IsHeadquarters && !b.IsDeleted)
                .Select(b => b.Id)
                .FirstAsync(ct);

            // DefaultBranchOfficeId reassign → HQ
            await ctx.Users
                .Where(u => u.DefaultBranchOfficeId == sourceId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.DefaultBranchOfficeId, (int?)hqId), ct);

            // Junction rows silinir
            await ctx.UserBranchOffices
                .Where(j => j.BranchOfficeId == sourceId)
                .ExecuteDeleteAsync(ct);

            // LastSelectedBranchOfficeId temizlenir (remember me olanlar HQ fallback'ine düşer)
            await ctx.Users
                .Where(u => u.LastSelectedBranchOfficeId == sourceId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.LastSelectedBranchOfficeId, (int?)null)
                    .SetProperty(x => x.RememberLastBranchOffice, false), ct);

            // Request'i onayla
            var trackedRequest = await ctx.BranchOfficeDeletionRequests.AsTracking()
                .FirstAsync(r => r.Id == requestId, ct);
            trackedRequest.Status = BranchOfficeDeletionRequestStatus.Approved;
            trackedRequest.ApprovedByUserId = approvedBy;
            trackedRequest.ReviewedAt = DateTimeOffset.UtcNow;

            if (notificationFlags.Value.PublishEnabled)
                ctx.AddDomainEvent(new BranchOfficeApprovalApprovedEvent(
                    requestId,
                    sourceId,
                    approvedBy));

            await ctx.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        // ── Audit log (commit sonrası) ─────────────────────────────────────
        string approverName;
        await using (var dbContext = await contextFactory.CreateDbContextAsync(ct))
        {
            approverName = (await dbContext.Users.AsNoTracking()
                .Where(u => u.Id == approvedBy)
                .Select(u => u.FullName ?? u.UserName ?? "")
                .FirstOrDefaultAsync(ct)) ?? "";
        }

        await applicationLogManager.AddLog(
            $"{branchName} şubesi için silme talebi onaylandı, onaylayan: {approverName}",
            LogType.BranchDeletion, LogAction.RequestApprove,
            "BranchOffice", sourceId.ToString(),
            new { requestId, approvedBy, transferredItemCount = transferredItems.Count }, ct);

        // Transfer edilen her item için tek satır Türkçe template log
        if (transferredItems.Count > 0)
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
            var variantIds = transferredItems.Select(t => t.VariantId).Distinct().ToList();
            var productTitles = await dbContext.ProductVariants.AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .Select(v => new { v.Id, Title = v.Product.Title ?? "" })
                .ToDictionaryAsync(x => x.Id, x => x.Title, ct);

            var targetName = targetId.HasValue
                ? (await dbContext.BranchOffices.AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(b => b.Id == targetId.Value)
                    .Select(b => b.Name ?? "")
                    .FirstOrDefaultAsync(ct)) ?? ""
                : "";

            foreach (var item in transferredItems)
            {
                var title = productTitles.TryGetValue(item.VariantId, out var t) ? t : "";
                await applicationLogManager.AddLog(
                    $"{item.Quantity} adet {title} {branchName}'ten {targetName}'e aktarıldı, onaylayan: {approverName}",
                    LogType.StockTransfer, LogAction.Transfer,
                    "BranchOffice", sourceId.ToString(), null, ct);
            }
        }

        // Notify requester
        try
        {
            await notificationManager.SendNotification(
                "Silme talebi onaylandı",
                $"{branchName} için açtığınız silme talebi onaylandı.",
                NotificationSeverity.Info,
                NotificationCategory.Sistem,
                [request.RequestedByUserId],
                actionUrl: $"/branch-office-deletion-requests/{requestId}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Approve notification dispatch failed (non-critical)");
        }

        return new SuccessResult("Silme talebi onaylandı ve şube silindi.");
    }

    // ─── RejectAsync ───────────────────────────────────────────────────────

    public async Task<IResult> RejectAsync(int requestId, Guid rejectedBy, string reason, CancellationToken ct = default)
    {
        var dto = new RejectDeletionRequestDto(requestId, reason);
        var validationResult = await rejectValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
            return new ErrorResult(string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage)));

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var request = await dbContext.BranchOfficeDeletionRequests.AsTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request is null)
            return new ErrorResult("Silme talebi bulunamadı.");
        if (request.Status != BranchOfficeDeletionRequestStatus.Pending)
            return new ErrorResult("Bu talep zaten sonuçlandırılmış.");

        request.Status = BranchOfficeDeletionRequestStatus.Rejected;
        request.ApprovedByUserId = rejectedBy;
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.RejectionReason = reason;

        // Denormalized cache'i temizle — yeni talep açılabilmesi için
        await dbContext.BranchOffices
            .Where(b => b.Id == request.BranchOfficeId)
            .ExecuteUpdateAsync(b => b.SetProperty(x => x.DeletionRequestId, (int?)null), ct);

        if (notificationFlags.Value.PublishEnabled)
            dbContext.AddDomainEvent(new BranchOfficeApprovalRejectedEvent(
                requestId,
                request.BranchOfficeId,
                rejectedBy,
                reason ?? string.Empty));

        await dbContext.SaveChangesAsync(ct);

        string branchName = await dbContext.BranchOffices.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(b => b.Id == request.BranchOfficeId)
            .Select(b => b.Name ?? "")
            .FirstOrDefaultAsync(ct) ?? "";

        await applicationLogManager.AddLog(
            $"{branchName} şubesi için silme talebi reddedildi. Gerekçe: {reason}",
            LogType.BranchDeletion, LogAction.RequestReject,
            "BranchOffice", request.BranchOfficeId.ToString(),
            new { requestId, rejectedBy, reason }, ct);

        try
        {
            await notificationManager.SendNotification(
                "Silme talebi reddedildi",
                $"{branchName} için açtığınız silme talebi reddedildi. Gerekçe: {reason}",
                NotificationSeverity.Warning,
                NotificationCategory.Sistem,
                [request.RequestedByUserId],
                actionUrl: $"/branch-office-deletion-requests/{requestId}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reject notification dispatch failed (non-critical)");
        }

        return new SuccessResult("Silme talebi reddedildi.");
    }

    // ─── Queries ───────────────────────────────────────────────────────────

    public async Task<IDataResult<Pageable<BranchOfficeDeletionRequestListDto>>> GetPagedAsync(
        int pageIndex = 0, int pageSize = 50,
        BranchOfficeDeletionRequestStatus? status = null,
        CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var query = dbContext.BranchOfficeDeletionRequests.AsNoTracking().AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(r => new BranchOfficeDeletionRequestListDto(
                r.Id,
                r.BranchOfficeId,
                dbContext.BranchOffices.IgnoreQueryFilters()
                    .Where(b => b.Id == r.BranchOfficeId).Select(b => b.Name ?? "").FirstOrDefault() ?? "",
                (int)r.Status,
                StatusText(r.Status),
                r.RequestedByUser.FullName ?? r.RequestedByUser.UserName ?? "",
                r.RequestedAt,
                r.HasStockTransfer,
                r.TransferTargetBranchOfficeId,
                r.TransferTargetBranchOfficeId.HasValue
                    ? dbContext.BranchOffices.IgnoreQueryFilters()
                        .Where(b => b.Id == r.TransferTargetBranchOfficeId.Value).Select(b => b.Name ?? "").FirstOrDefault()
                    : null,
                r.ReviewedAt,
                r.RejectionReason))
            .ToListAsync(ct);

        return new SuccessDataResult<Pageable<BranchOfficeDeletionRequestListDto>>(
            new Pageable<BranchOfficeDeletionRequestListDto>(items, pageIndex, pageSize, total));
    }

    public async Task<IDataResult<BranchOfficeDeletionRequestDetailDto>> GetByIdAsync(
        int requestId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        var r = await dbContext.BranchOfficeDeletionRequests.AsNoTracking()
            .Include(x => x.RequestedByUser)
            .Include(x => x.ApprovedByUser)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == requestId, ct);

        if (r is null)
            return new ErrorDataResult<BranchOfficeDeletionRequestDetailDto>(null!, "Talep bulunamadı.");

        var branchName = await dbContext.BranchOffices.IgnoreQueryFilters()
            .Where(b => b.Id == r.BranchOfficeId).Select(b => b.Name ?? "").FirstOrDefaultAsync(ct) ?? "";

        string? targetName = null;
        if (r.TransferTargetBranchOfficeId.HasValue)
        {
            targetName = await dbContext.BranchOffices.IgnoreQueryFilters()
                .Where(b => b.Id == r.TransferTargetBranchOfficeId.Value)
                .Select(b => b.Name ?? "").FirstOrDefaultAsync(ct);
        }

        // Snapshot items → title eşleştirmesi
        var snapshotVariantIds = r.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var snapshotTitles = await dbContext.ProductVariants.AsNoTracking()
            .Where(v => snapshotVariantIds.Contains(v.Id))
            .Select(v => new { v.Id, Title = v.Product.Title ?? "", Barcode = v.Barcode ?? "" })
            .ToDictionaryAsync(x => x.Id, x => (x.Title, x.Barcode), ct);

        var snapshotDtos = r.Items
            .Select(i => new BranchOfficeDeletionRequestItemDto(
                i.ProductVariantId,
                snapshotTitles.TryGetValue(i.ProductVariantId, out var info) ? info.Title : "",
                snapshotTitles.TryGetValue(i.ProductVariantId, out var b) ? b.Barcode : "",
                i.Quantity))
            .ToList();

        // Live items — current stock
        var liveDtos = await dbContext.BranchOfficeStocks.AsNoTracking()
            .Where(s => s.BranchOfficeId == r.BranchOfficeId
                        && s.ProductVariantId.HasValue
                        && (s.FirstTotalStock - s.SoldQuantity) > 0)
            .Select(s => new BranchOfficeDeletionRequestItemDto(
                s.ProductVariantId!.Value,
                s.ProductVariant!.Product.Title ?? "",
                s.ProductVariant.Barcode ?? "",
                s.FirstTotalStock - s.SoldQuantity))
            .ToListAsync(ct);

        var detail = new BranchOfficeDeletionRequestDetailDto(
            r.Id,
            r.BranchOfficeId,
            branchName,
            (int)r.Status,
            StatusText(r.Status),
            r.RequestedByUserId,
            r.RequestedByUser.FullName ?? r.RequestedByUser.UserName ?? "",
            r.RequestedAt,
            r.ApprovedByUserId,
            r.ApprovedByUser?.FullName ?? r.ApprovedByUser?.UserName,
            r.ReviewedAt,
            r.RejectionReason,
            r.HasStockTransfer,
            r.TransferTargetBranchOfficeId,
            targetName,
            snapshotDtos,
            liveDtos);

        return new SuccessDataResult<BranchOfficeDeletionRequestDetailDto>(detail);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        return await dbContext.BranchOfficeDeletionRequests
            .CountAsync(r => r.Status == BranchOfficeDeletionRequestStatus.Pending, ct);
    }

    // ─── Business rule helpers ─────────────────────────────────────────────

    private static async Task<IResult> CheckOfficeExistsAndActiveAsync(
        IntegrationDbContext db, int branchId, CancellationToken ct)
    {
        var exists = await db.BranchOffices.AnyAsync(b => b.Id == branchId && !b.IsDeleted, ct);
        return exists ? new SuccessResult() : new ErrorResult("Şube bulunamadı veya pasif.");
    }

    private static async Task<IResult> CheckNotHQAsync(
        IntegrationDbContext db, int branchId, CancellationToken ct)
    {
        var isHq = await db.BranchOffices.AnyAsync(b => b.Id == branchId && b.IsHeadquarters, ct);
        return isHq ? new ErrorResult("Merkez ofis silinemez.") : new SuccessResult();
    }

    private static async Task<IResult> CheckNoPendingRequestAsync(
        IntegrationDbContext db, int branchId, CancellationToken ct)
    {
        var hasPending = await db.BranchOfficeDeletionRequests
            .AnyAsync(r => r.BranchOfficeId == branchId
                           && r.Status == BranchOfficeDeletionRequestStatus.Pending, ct);
        return hasPending
            ? new ErrorResult("Bu şube için zaten bekleyen bir silme talebi var.")
            : new SuccessResult();
    }

    private static async Task<IResult> CheckNoActivePosSessionAsync(
        IntegrationDbContext db, int branchId, CancellationToken ct)
    {
        var has = await db.POSSessions
            .AnyAsync(s => s.BranchOfficeId == branchId && s.Status == POSSessionStatus.Open, ct);
        return has
            ? new ErrorResult("Bu şubede açık POS oturumu var. Lütfen önce oturumu kapatın.")
            : new SuccessResult();
    }

    private static async Task<IResult> CheckNotDefaultMarketplaceStockAsync(
        IntegrationDbContext db, int branchId, CancellationToken ct)
    {
        var isDefault = await db.BranchOffices
            .AnyAsync(b => b.Id == branchId && b.IsDefaultMarketPlaceStock, ct);
        return isDefault
            ? new ErrorResult("Bu şube varsayılan marketplace deposudur. Önce başka bir şubeyi varsayılan yapın.")
            : new SuccessResult();
    }

    private static async Task<IResult> CheckAtLeastOneActiveBranchRemainsAsync(
        IntegrationDbContext db, int excludingBranchId, CancellationToken ct)
    {
        var count = await db.BranchOffices.CountAsync(b => !b.IsDeleted && b.Id != excludingBranchId, ct);
        return count >= 1
            ? new SuccessResult()
            : new ErrorResult("Sistemde en az bir aktif şube kalmak zorunda.");
    }

    private static string StatusText(BranchOfficeDeletionRequestStatus status) => status switch
    {
        BranchOfficeDeletionRequestStatus.Pending => "Beklemede",
        BranchOfficeDeletionRequestStatus.Approved => "Onaylandı",
        BranchOfficeDeletionRequestStatus.Rejected => "Reddedildi",
        _ => "-"
    };
}
