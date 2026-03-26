namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontGiftCardTransaction : BaseEntity
{
    public int Id { get; set; }
    public int GiftCardId { get; set; }
    public StorefrontGiftCard GiftCard { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public decimal Amount { get; set; } // positive=load, negative=use
    public string TransactionType { get; set; } = null!; // Purchase, Use, Refund
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
}
