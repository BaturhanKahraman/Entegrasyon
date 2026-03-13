using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductEditDetailDto(
    Guid Id,
    string Title,
    string Description,
    string StockCode,
    string Season,
    string Year,
    int BrandId,
    int CategoryId,
    List<ProductVariantEditDetailDto> ProductVariants,
    List<AttributeKeyValueDto> AttributeKeyValues
);
