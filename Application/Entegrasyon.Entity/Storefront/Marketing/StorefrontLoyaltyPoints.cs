namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontLoyaltyPoints : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public int TotalEarned { get; set; }
    public int TotalSpent { get; set; }
    public int CurrentBalance { get; set; } // computed: TotalEarned - TotalSpent
}
