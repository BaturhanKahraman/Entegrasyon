using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Entity.Products;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class ProductMapper
{
    // Product
    [MapperIgnoreTarget(nameof(Product.SearchVector))]
    public partial Product MapToEntity(AddProductDto dto);

    public partial AddProductDto MapToDto(Product entity);

    [MapperIgnoreTarget(nameof(Product.SearchVector))]
    public partial Product MapToEntity(ProductEditDetailDto dto);

    // ProductVariant
    [MapProperty(nameof(AddProductVariantDto.VatRate), nameof(ProductVariant.VatRate),
                 Use = nameof(NullableDecimalToDecimal))]
    public partial ProductVariant MapToEntity(AddProductVariantDto dto);

    public partial AddProductVariantDto MapToDto(ProductVariant entity);

    public partial ProductVariant MapToEntity(ProductVariantEditDetailDto dto);

    // BranchOfficeStock
    public partial BranchOfficeStock MapToEntity(AddBranchOfficeStockDto dto);
    public partial AddBranchOfficeStockDto MapToDto(BranchOfficeStock entity);
    public partial BranchOfficeStock MapToEntity(EditBranchOfficeStockDto dto);
    public partial EditBranchOfficeStockDto MapToEditDto(BranchOfficeStock entity);

    private static decimal NullableDecimalToDecimal(decimal? value) => value ?? 0m;
}
