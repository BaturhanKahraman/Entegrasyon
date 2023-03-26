using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public record ProductVariantEditDetailDto(
    Guid Id,
    decimal DimensionalWeight,
string CurrencyType,
string Barcode,
decimal ListPrice,
decimal SalePrice,
decimal CostPrice,
decimal VatRate,
List<EditBranchOfficeStockDto> BranchOfficeStocks,
    List<EditableImageDto> UploadedImages,
    List<VariantAttributeDto> VariantAttributes
    );