namespace Entegrasyon.Entity.Dtos.Product.Discount;

public sealed record DiscountPreviewDto(
    Guid ProductId,
    string ProductTitle,
    List<VariantDiscountInfoDto> Variants,
    List<MarketplacePriceInfoDto> MarketplacePrices
);

public sealed record VariantDiscountInfoDto(
    Guid VariantId,
    string Barcode,
    string Label,
    decimal CostPrice,
    decimal ListPrice,
    decimal SalePrice,
    decimal ECommercePrice,
    decimal VatRate,
    decimal ProfitMargin,
    decimal ProfitMarginPercent
);

public sealed record MarketplacePriceInfoDto(
    int MarketPlaceId,
    string MarketPlaceName,
    Guid VariantId,
    string Barcode,
    decimal MarketplaceListPrice,
    decimal MarketplaceSalePrice
);
