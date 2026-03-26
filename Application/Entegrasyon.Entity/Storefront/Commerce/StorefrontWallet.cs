namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontWallet : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public decimal Balance { get; set; }
    public ICollection<StorefrontWalletTransaction> Transactions { get; set; } = new List<StorefrontWalletTransaction>();
}
