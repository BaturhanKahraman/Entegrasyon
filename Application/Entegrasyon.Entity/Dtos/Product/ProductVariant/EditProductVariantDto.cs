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
List<EditBranchOfficeStockDto> BranchOfficeStocks,
    IEnumerable<EditableImageDto> UploadedImages
    );