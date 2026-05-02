namespace Entegrasyon.Entity.Printing;

public enum PrintBatchStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}

public sealed class PrintBatch : BaseEntity
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public int? FulfilledByDeviceId { get; set; }
    public PrintBatchStatus Status { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }

    public List<PrintBatchItem> Items { get; set; } = [];
}
