namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontPushSubscription : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? CustomerId { get; set; }
    public string Endpoint { get; set; } = null!;
    public string P256dhKey { get; set; } = null!;
    public string AuthKey { get; set; } = null!;
}
