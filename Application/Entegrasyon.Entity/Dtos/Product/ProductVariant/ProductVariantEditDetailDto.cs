using Entegrasyon.Entity.Dtos.Attributes;

namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public record ProductVariantEditDetailDto(
    Guid Id,
    decimal DimensionalWeight,
    string CurrencyType,
    string Barcode,
    string? Name,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal ECommercePrice,
    decimal VatRate,
    List<EditBranchOfficeStockDto> BranchOfficeStocks,
    List<EditableImageDto> UploadedImages,
    List<VariantAttributeDto> VariantAttributes
);
