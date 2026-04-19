namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public record ProductVariantSaleSearchDto(
    Guid ProductVariantId,
    string ProductName,
    string DisplayName,
    string PrimaryImage,
    decimal TaxPercentage,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    int TotalStock,
    string CategoryName
);