namespace Entegrasyon.Entity.Storefront;

public sealed class SellerTransaction : BaseEntity
{
    public int Id { get; set; }
    public int SellerId { get; set; }
    public Seller Seller { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = null!; // Sale, Commission, Payout, Refund
    public string? Description { get; set; }
    public decimal BalanceAfter { get; set; }
}
