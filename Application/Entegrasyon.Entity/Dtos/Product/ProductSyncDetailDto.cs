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
    string? StatusMessage,
    string? ExternalProductId = null,
    long? ContentId = null,
    bool? IsApproved = null,
    bool? IsArchived = null,
    /// <summary>
    /// Indicates whether the marketplace has valid API credentials configured.
    /// Used by the UI to enable/disable marketplace status cards.
    /// </summary>
    bool HasCredentials = false);
