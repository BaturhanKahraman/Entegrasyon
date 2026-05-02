using Entegrasyon.Entity.Printing;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public sealed record PrintBatchItemDraft(
    int Order,
    string PrinterLanguage,
    string ZplContent,
    byte[]? RawBytes,
    string Description);

public sealed record PrintBatchCreatedResult(
    Guid BatchId,
    int TotalItems,
    string OneTimeBatchToken);

public sealed record PrintBatchItemStatusUpdate(
    int Order,
    bool Printed,
    string? ErrorMessage);

public interface IPrintBatchManager
{
    Task<IDataResult<PrintBatchCreatedResult>> CreateAsync(
        int tenantId, Guid userId, IReadOnlyList<PrintBatchItemDraft> items);

    Task<IDataResult<PrintBatch>> GetForDeviceAsync(
        Guid batchId, int deviceId, int tenantId);

    Task<IResult> RecordItemStatusAsync(
        Guid batchId, int deviceId, IReadOnlyList<PrintBatchItemStatusUpdate> updates);
}
