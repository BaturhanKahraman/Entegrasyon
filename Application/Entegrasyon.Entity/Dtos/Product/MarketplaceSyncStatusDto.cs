namespace Entegrasyon.Entity.Dtos.Product;

public enum MarketplaceSyncState
{
    NeverSynced,
    Waiting,
    Processing,
    OutOfSync,
    Synced,
    Failed,
    Rejected,
    Removed
}

public sealed record MarketplaceSyncStatusDto(
    MarketplaceSyncState State,
    DateTimeOffset? LastSyncedAt,
    string? BatchRequestId,
    string? StatusMessage
);
