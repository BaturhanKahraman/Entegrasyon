using Entegrasyon.Entity.Customers;

namespace Entegrasyon.Entity.Storefront;

public sealed class Cart : BaseEntity
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? SessionId { get; set; }
    public string? CouponCode { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
