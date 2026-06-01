namespace Entegrasyon.Entity.Storefront;

public enum WalletTransactionType
{
    Refund = 0,
    OrderPayment = 1,
    Promotion = 2,
    TopUp = 3,
    Cashback = 4
}

public sealed class StorefrontWalletTransaction : BaseEntity
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public StorefrontWallet Wallet { get; set; } = null!;
    public decimal Amount { get; set; }
    public WalletTransactionType TransactionType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Description { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
}
