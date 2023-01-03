using Entegrasyon.Entity.Categories;
using NpgsqlTypes;
using Shared.Entity;

namespace Entegrasyon.Entity.Products;

public sealed class Product : BaseEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string StockCode { get; set; }
    public int? BrandId { get; set; }
    public Brand Brand { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public IEnumerable<ProductVariant> ProductVariants { get; set; }
    public IEnumerable<AttributeKeyValue> AttributeKeyValues { get; set; }
    public NpgsqlTsVector SearchVector { get; set; }


}