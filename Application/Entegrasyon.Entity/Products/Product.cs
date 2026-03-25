using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using NpgsqlTypes;

namespace Entegrasyon.Entity.Products;

public sealed class Product : BaseEntity
{
    //yılı
    // sezon
    // firma
    // 
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? StockCode { get; set; }
    public string? Season { get; set; }
    public string? Year { get; set; }
    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public ICollection<AttributeKeyValue> AttributeKeyValues { get; set; } = new List<AttributeKeyValue>();
    public ICollection<ProductMarketplace> ProductMarketplaces { get; set; } = new List<ProductMarketplace>();
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    // SEO
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoSlug { get; set; }
    public string? SeoKeywords { get; set; }
}