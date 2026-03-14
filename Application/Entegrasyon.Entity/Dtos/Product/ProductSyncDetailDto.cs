namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductSyncDetailDto(
    Guid ProductId,
    string Title,
    string StockCode,
    string BrandName,
    string CategoryName,
    int VariantCount,
    List<MarketplaceSyncItemDto> Marketplaces);

public sealed record MarketplaceSyncItemDto(
    int MarketPlaceId,
    string MarketPlaceName,
    MarketplaceSyncState SyncState,
    DateTimeOffset? LastSyncedAt,
    string? BatchRequestId,
    string? StatusMessage);
