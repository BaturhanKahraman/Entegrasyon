using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductDetailDto(
        Guid Id,
        string Title,
        string Description,
        string StockCode,
        string Season,
        string Year,
        int? BrandId,
        string BrandName,
        int CategoryId,
        string CategoryName,
        int TotalQuantity,
        int TotalSoldQuantity,
        IEnumerable<ProductVariantDetailDto> ProductVariantsDetails,
        IEnumerable<AttributeKeyValueDetailDto> AttributeKeyValueDetails,
        DateTimeOffset UpdatedAt
    );
