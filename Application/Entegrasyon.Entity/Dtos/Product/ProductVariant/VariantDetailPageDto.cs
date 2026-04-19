namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public record VariantDetailPageDto(
    Guid ProductId,
    string ProductTitle,
    string DisplayName,
    Guid VariantId,
    string Barcode,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal ECommercePrice,
    decimal VatRate,
    decimal DimensionalWeight,
    string CurrencyType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<StockDetailDto> StockDetails,
    List<VariantImageInfo> Images,
    List<VariantAttributeInfo> Attributes,
    List<MarketplaceVariantInfo> Marketplaces
);

public record VariantImageInfo(int Id, string Src, bool IsMain);

public record VariantAttributeInfo(string Name, string Value, bool IsVarianter, bool IsSlicer);

public record MarketplaceVariantInfo(
    int MarketPlaceId,
    string MarketPlaceName,
    MarketplaceSyncState SyncState,
    string SyncBadgeClass,
    DateTimeOffset? LastSyncedAt,
    string? StatusMessage,
    decimal? ListPriceOverride,
    decimal? SalePriceOverride
);
