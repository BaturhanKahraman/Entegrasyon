using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.Entity.Dtos.Product;

public record EditProductDto(
    Guid Id,
    string Title,
    string Description,
    string StockCode,
    int BrandId,
    int CategoryId ,
    List<EditProductVariantDto> ProductVariants
);