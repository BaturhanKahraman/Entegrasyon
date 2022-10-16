using Shared.Entity;

namespace Entegrasyon.Entity.Sales;

public sealed class DiscountVoucher: BaseEntity
{
    public int Id { get; set; }
    public double Percentage { get; set; }
    public decimal Amount { get; set; }
    
}