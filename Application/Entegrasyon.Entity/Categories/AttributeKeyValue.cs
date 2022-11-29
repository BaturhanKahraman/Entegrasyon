using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity.Categories;

public sealed class AttributeKeyValue :BaseEntity
{
    public int CategoryAttributeId { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; }
    public string CustomValue { get; set; }
    public int? AttributeValueId { get; set; }
    public CategoryAttributeValue AttributeValue { get; set; }
    
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; }
}