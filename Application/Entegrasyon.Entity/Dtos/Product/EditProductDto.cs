using Entegrasyon.Entity.Dtos.Attributes;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed record EditProductDto(
    Guid Id,
    string Title,
    string Description,
    string StockCode,
    string Season,
    string Year,
    int BrandId,
    int CategoryId,
    List<EditProductVariantDto> Variants,
    List<AttributeKeyValueDto> AttributeKeyValues,
    List<int> DeletedImageIds
);
