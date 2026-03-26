namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontReferral : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int ReferrerCustomerId { get; set; }
    public string ReferralCode { get; set; } = null!; // unique per tenant
    public int? ReferredCustomerId { get; set; }
    public ReferralStatus Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public enum ReferralStatus { Pending, Registered, Completed, Rewarded }
