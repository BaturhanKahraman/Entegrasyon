namespace Entegrasyon.Entity.Notifications;

public sealed class DeadLetterOutbox : BaseEntity
{
    public long Id { get; set; }
    public long OriginalOutboxId { get; set; }
    public int TenantId { get; set; }
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string FinalError { get; set; } = null!;
    public int TotalRetryCount { get; set; }
    public DateTimeOffset MovedAt { get; set; }
}
