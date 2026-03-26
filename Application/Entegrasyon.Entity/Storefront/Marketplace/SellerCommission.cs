namespace Entegrasyon.Entity.Storefront;

public sealed class SellerCommission : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int SellerId { get; set; }
    public Seller Seller { get; set; } = null!;
    public int? CategoryId { get; set; }
    public decimal CommissionRate { get; set; }
}
