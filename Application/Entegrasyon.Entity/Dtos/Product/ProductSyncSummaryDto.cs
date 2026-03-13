namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductSyncSummaryDto(
    int TotalProducts,
    int SyncedCount,
    int PendingCount,
    int FailedCount,
    int NeverSyncedCount);
