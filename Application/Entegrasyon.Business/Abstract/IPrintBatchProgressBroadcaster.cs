namespace Entegrasyon.Business.Abstract;

public sealed record PrintBatchProgressEvent(
    Guid BatchId,
    int CompletedItems,
    int TotalItems,
    string Status,
    string? FailureReason);

public interface IPrintBatchProgressBroadcaster
{
    void Publish(PrintBatchProgressEvent payload);
    IAsyncEnumerable<PrintBatchProgressEvent> Subscribe(Guid batchId, CancellationToken ct);
}
