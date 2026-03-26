namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontReview : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Guid ProductId { get; set; }
    public int CustomerId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Title { get; set; }
    public string Comment { get; set; } = null!;
    public bool IsApproved { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public string? ReplyText { get; set; }
    public DateTimeOffset? RepliedAt { get; set; }
}
