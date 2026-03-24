namespace Entegrasyon.Desktop.Data;

/// <summary>
/// Outbox pattern queue entry for offline-to-server sync.
/// </summary>
public class SyncQueue
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public SyncAction Action { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsSynced { get; set; }
    public DateTimeOffset? SyncedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

public enum SyncAction
{
    Create,
    Update
}
