namespace Entegrasyon.Entity.Dtos.Product;

public sealed record ProductDetailDto(Guid Id,
    string Title,
    string? Description,
    string? StockCode,
    string? BrandName,
    string? CategoryName,
    int TotalQuantity,
    int TotalSoldQuantity,
    int TotalCurrentStock,
    int VariantCount);