using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public record EditProductVariantDto(
    Guid Id,
    decimal DimensionalWeight,
string CurrencyType,
string Barcode,
decimal ListPrice,
decimal SalePrice,
decimal CostPrice,
decimal VatRate,
ICollection<AttributeKeyValue> AttributeKeyValues,
List<EditBranchOfficeStockDto> BranchOfficeStocks,
ICollection<EditableImageDto> UploadedImages
    );