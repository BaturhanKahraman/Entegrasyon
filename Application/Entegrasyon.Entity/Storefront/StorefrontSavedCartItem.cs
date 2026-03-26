using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontSavedCartItem : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public decimal OriginalPrice { get; set; }
    public DateTimeOffset SavedAt { get; set; }
}
