using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record EditProductDto(
    Guid Id,
    string Title,
    string Description,
    string StockCode,
    int BrandId,
    int CategoryId ,
    List<EditProductVariantDto> ProductVariants,
    List<AttributeKeyValue> AttributeKeyValues
);