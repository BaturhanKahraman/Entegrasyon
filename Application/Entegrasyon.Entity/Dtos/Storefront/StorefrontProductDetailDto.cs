namespace Entegrasyon.Entity.Dtos.Storefront;

public record StorefrontProductDetailDto(
    Guid Id, string Title, string? Description, string? StockCode,
    string? SeoSlug, string? SeoTitle, string? SeoDescription,
    string? BrandName, string? BrandSlug,
    string CategoryName, string? CategorySlug, int CategoryId,
    decimal MinPrice, decimal MaxPrice,
    List<StorefrontVariantDto> Variants,
    List<StorefrontAttributeDto> Attributes,
    List<BreadcrumbItemDto> Breadcrumbs);

public record StorefrontVariantDto(
    Guid Id, string? Barcode, decimal ListPrice, decimal SalePrice, int Stock,
    List<StorefrontVariantAttributeDto> Attributes, List<string> ImageUrls);

public record StorefrontVariantAttributeDto(string AttributeKey, string AttributeValue);

public record StorefrontAttributeDto(string Key, string HumanizedKey, string Value);
