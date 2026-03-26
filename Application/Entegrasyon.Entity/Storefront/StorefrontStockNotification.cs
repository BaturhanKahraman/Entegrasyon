namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontStockNotification : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Guid ProductVariantId { get; set; }
    public string Email { get; set; } = null!;
    public bool IsNotified { get; set; }
    public DateTimeOffset? NotifiedAt { get; set; }
}
