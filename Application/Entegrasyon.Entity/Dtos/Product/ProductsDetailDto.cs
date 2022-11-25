namespace Entegrasyon.Entity.Dtos.Product;

public sealed class ProductsDetailDto
{
    public ProductsDetailDto(Guid id,
        string title,
        string? description,
        string? stockCode,
        string? brandName,
        string? categoryName,
        int totalQuantity,
        int totalSoldQuantity,
        int variantCount)
    {
        this.Id = id;
        this.Title = title;
        this.Description = description;
        this.StockCode = stockCode;
        this.BrandName = brandName;
        this.CategoryName = categoryName;
        this.TotalQuantity = totalQuantity;
        this.TotalSoldQuantity = totalSoldQuantity;
        this.VariantCount = variantCount;
    }

    public Guid Id { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public string StockCode { get; init; }
    public string BrandName { get; init; }
    public string CategoryName { get; init; }
    public int TotalQuantity { get; init; }
    public int TotalSoldQuantity { get; init; }
    public int TotalCurrentStock => TotalQuantity - TotalSoldQuantity;
    public int VariantCount { get; init; }
    
}