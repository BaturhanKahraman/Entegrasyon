namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductSyncListItemDto(
    Guid ProductId,
    string Title,
    string StockCode,
    string BrandName,
    string CategoryName,
    int VariantCount,
    MarketplaceSyncState SyncState,
    DateTimeOffset? LastSyncedAt,
    string? StatusMessage);
