using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontWishlistItem : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTimeOffset AddedAt { get; set; }
}
