namespace Entegrasyon.Entity.Dtos.Product.ProductVariant;

public sealed record ProductVariantDetailDto(Guid Id,
    string Barcode,
    decimal DeminsionalWeight,
    string CurrencyType,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal ECommercePrice,
    decimal VatRate,
    string[] imageLinks,
    IEnumerable<StockDetailDto> StockDetails
    );