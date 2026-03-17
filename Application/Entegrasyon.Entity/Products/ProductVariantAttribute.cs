namespace Entegrasyon.Entity.Products;

public sealed class ProductVariantAttribute
{
    public int? CategoryAttributeValueId { get; set; }
    public string CategoryAttributeValue { get; set; } = null!;
    public string CustomValue { get; set; } = null!;
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
}