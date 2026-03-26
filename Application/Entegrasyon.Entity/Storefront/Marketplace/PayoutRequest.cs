namespace Entegrasyon.Entity.Storefront;

public sealed class PayoutRequest : BaseEntity
{
    public int Id { get; set; }
    public int SellerId { get; set; }
    public Seller Seller { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Iban { get; set; } = null!;
    public PayoutStatus Status { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Note { get; set; }
}

public enum PayoutStatus { Pending, Processing, Completed, Rejected }
