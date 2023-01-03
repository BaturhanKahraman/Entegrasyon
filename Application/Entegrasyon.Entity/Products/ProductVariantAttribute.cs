using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Products;

public sealed class ProductVariantAttribute
{
    public int? CategoryAttributeValueId { get; set; }
    public string CustomValue { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
}