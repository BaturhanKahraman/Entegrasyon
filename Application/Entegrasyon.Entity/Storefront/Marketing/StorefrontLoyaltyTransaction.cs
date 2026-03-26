namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontLoyaltyTransaction : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public int Points { get; set; } // positive=earn, negative=spend
    public string TransactionType { get; set; } = null!; // Purchase, Welcome, Review, Referral, Redemption
    public string? ReferenceId { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
