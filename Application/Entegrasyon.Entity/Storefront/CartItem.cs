using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Storefront;

public sealed class CartItem : BaseEntity
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}
