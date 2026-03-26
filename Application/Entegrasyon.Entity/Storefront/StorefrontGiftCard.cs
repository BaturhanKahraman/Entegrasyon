namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontGiftCard : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Code { get; set; } = null!;
    public decimal InitialAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int? PurchasedByCustomerId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? SenderMessage { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public GiftCardStatus Status { get; set; }
}

public enum GiftCardStatus { Active, Used, Expired, Cancelled }
