namespace Entegrasyon.Entity.BulkOperations;

public sealed class BulkOperationLog : BaseEntity
{
    public long Id { get; set; }
    public BulkOperationType OperationType { get; set; }
    public string FileName { get; set; } = null!;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public BulkOperationStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid StartedByUserId { get; set; }
    public string? ErrorDetails { get; set; }
}
