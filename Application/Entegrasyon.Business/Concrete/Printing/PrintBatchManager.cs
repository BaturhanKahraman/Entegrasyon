using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Printing;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Printing;

public class PrintBatchManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IBatchTokenService batchTokenService,
    IPrintBatchProgressBroadcaster progressBroadcaster,
    IApplicationLogManager applicationLogManager,
    ILogger<PrintBatchManager> logger) : IPrintBatchManager
{
    private static readonly TimeSpan TokenTtl = TimeSpan.FromSeconds(60);

    public async Task<IDataResult<PrintBatchCreatedResult>> CreateAsync(
        int tenantId, Guid userId, IReadOnlyList<PrintBatchItemDraft> items)
    {
        if (items is null || items.Count == 0)
            return new ErrorDataResult<PrintBatchCreatedResult>(default!, "Yazdırılacak öğe bulunmuyor.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var batch = new PrintBatch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedByUserId = userId,
            Status = PrintBatchStatus.Pending,
            TotalItems = items.Count,
            CompletedItems = 0
        };

        foreach (var draft in items)
        {
            batch.Items.Add(new PrintBatchItem
            {
                PrintBatchId = batch.Id,
                Order = draft.Order,
                PrinterLanguage = draft.PrinterLanguage,
                ZplContent = draft.ZplContent,
                RawBytes = draft.RawBytes,
                Description = draft.Description,
                Status = PrintBatchItemStatus.Pending
            });
        }

        dbContext.PrintBatches.Add(batch);
        await dbContext.SaveChangesAsync();

        var token = batchTokenService.Generate(new BatchTokenPayload(
            BatchId: batch.Id,
            TenantId: tenantId,
            UserId: userId,
            ExpiresAt: DateTimeOffset.UtcNow.Add(TokenTtl),
            Nonce: Guid.NewGuid().ToString("N")));

        await applicationLogManager.AddLog(
            $"Yazdırma batch oluşturuldu: {batch.Id} ({items.Count} öğe)",
            LogType.Settings, LogAction.Add);
        logger.LogInformation(
            "PrintBatch created: {BatchId} Tenant={TenantId} ItemCount={Count}",
            batch.Id, tenantId, items.Count);

        return new SuccessDataResult<PrintBatchCreatedResult>(
            new PrintBatchCreatedResult(batch.Id, items.Count, token),
            "Yazdırma işi oluşturuldu.");
    }

    public async Task<IDataResult<PrintBatch>> GetForDeviceAsync(Guid batchId, int deviceId, int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var batch = await dbContext.PrintBatches
            .AsTracking()
            .Include(b => b.Items.OrderBy(i => i.Order))
            .FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);

        if (batch is null)
            return new ErrorDataResult<PrintBatch>(null!, "Yazdırma işi bulunamadı.");

        if (batch.TenantId != tenantId)
            return new ErrorDataResult<PrintBatch>(null!, "Yazdırma işi bu kiracıya ait değil.");

        if (batch.Status == PrintBatchStatus.Pending)
        {
            batch.Status = PrintBatchStatus.InProgress;
            batch.StartedAt = DateTimeOffset.UtcNow;
            batch.FulfilledByDeviceId = deviceId;
            await dbContext.SaveChangesAsync();
        }

        return new SuccessDataResult<PrintBatch>(batch, "Yazdırma işi getirildi.");
    }

    public async Task<IResult> RecordItemStatusAsync(
        Guid batchId, int deviceId, IReadOnlyList<PrintBatchItemStatusUpdate> updates)
    {
        if (updates is null || updates.Count == 0)
            return new ErrorResult("Güncelleme verisi yok.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var batch = await dbContext.PrintBatches
            .AsTracking()
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted);

        if (batch is null)
            return new ErrorResult("Yazdırma işi bulunamadı.");

        var now = DateTimeOffset.UtcNow;
        foreach (var update in updates)
        {
            var item = batch.Items.FirstOrDefault(i => i.Order == update.Order);
            if (item is null) continue;

            if (update.Printed)
            {
                item.Status = PrintBatchItemStatus.Printed;
                item.PrintedAt = now;
                item.ErrorMessage = null;
            }
            else
            {
                item.Status = PrintBatchItemStatus.Failed;
                item.ErrorMessage = update.ErrorMessage;
            }
        }

        batch.CompletedItems = batch.Items.Count(i => i.Status == PrintBatchItemStatus.Printed);
        var anyFailed = batch.Items.Any(i => i.Status == PrintBatchItemStatus.Failed);
        var allDone = batch.Items.All(i => i.Status != PrintBatchItemStatus.Pending);

        if (allDone)
        {
            batch.Status = anyFailed ? PrintBatchStatus.Failed : PrintBatchStatus.Completed;
            batch.CompletedAt = now;
            if (anyFailed)
                batch.FailureReason = string.Join("; ",
                    batch.Items.Where(i => i.ErrorMessage is not null).Select(i => i.ErrorMessage));
        }
        else if (anyFailed)
        {
            batch.Status = PrintBatchStatus.Failed;
        }

        await dbContext.SaveChangesAsync();

        progressBroadcaster.Publish(new PrintBatchProgressEvent(
            BatchId: batch.Id,
            CompletedItems: batch.CompletedItems,
            TotalItems: batch.TotalItems,
            Status: batch.Status.ToString(),
            FailureReason: batch.FailureReason));

        logger.LogInformation(
            "PrintBatch status updated: {BatchId} Device={DeviceId} Completed={Completed}/{Total}",
            batchId, deviceId, batch.CompletedItems, batch.TotalItems);

        return new SuccessResult("Durum güncellendi.");
    }
}
