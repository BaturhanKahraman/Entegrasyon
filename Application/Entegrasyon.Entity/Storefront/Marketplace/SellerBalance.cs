namespace Entegrasyon.Entity.Storefront;

public sealed class SellerBalance : BaseEntity
{
    public int Id { get; set; }
    public int SellerId { get; set; }
    public Seller Seller { get; set; } = null!;
    public decimal TotalEarned { get; set; }
    public decimal TotalPaidOut { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal CurrentBalance { get; set; }
}
