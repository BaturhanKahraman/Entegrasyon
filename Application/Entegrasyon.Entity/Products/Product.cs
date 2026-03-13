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
    public string Title { get; set; }
    public string Description { get; set; }
    public string StockCode { get; set; }
    public string Season { get; set; }
    public string Year { get; set; }
    public int? BrandId { get; set; }
    public Brand Brand { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; }
    public ICollection<AttributeKeyValue> AttributeKeyValues { get; set; }
    public ICollection<ProductMarketplace> ProductMarketplaces { get; set; } = [];
    public NpgsqlTsVector SearchVector { get; set; }


}