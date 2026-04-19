namespace Entegrasyon.Entity.Dtos.Product;

public sealed record EditProductVariantDto(
    Guid Id,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal ECommercePrice,
    decimal DimensionalWeight,
    decimal VatRate,
    string CurrencyType,
    string? Name = null
);
