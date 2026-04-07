using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;

namespace Entegrasyon.Entity.Dtos.Product;

public sealed class AddProductDto
{
   
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string StockCode { get; set; } = null!;
    public string? Season { get; set; }
    public string? Year { get; set; }
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoSlug { get; set; }
    public string? SeoKeywords { get; set; }
    public IEnumerable<AttributeKeyValue> AttributeKeyValues { get; set; } = [];

    public IEnumerable<AddProductVariantDto> ProductVariants { get; set; } = [];
}
/*
 *     public string Title { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
 */