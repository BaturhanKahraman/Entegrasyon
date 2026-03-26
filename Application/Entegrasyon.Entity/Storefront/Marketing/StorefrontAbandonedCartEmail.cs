namespace Entegrasyon.Entity.Storefront;

public enum AbandonedCartEmailStatus
{
    Pending = 0,
    Sent = 1,
    Converted = 2
}

public sealed class StorefrontAbandonedCartEmail : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Guid CartId { get; set; }
    public int CustomerId { get; set; }
    public int EmailStep { get; set; } // 1, 2, 3
    public DateTimeOffset? SentAt { get; set; }
    public bool Converted { get; set; }
    public string? CouponCode { get; set; }
    public AbandonedCartEmailStatus Status { get; set; } = AbandonedCartEmailStatus.Pending;
}
