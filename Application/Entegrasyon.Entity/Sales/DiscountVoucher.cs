using Shared.Abstract.Entity;

namespace Entegrasyon.Entity.Sales;

public class DiscountVoucher:ApplicationEntity
{
    public double Percentage { get; set; }
    public decimal Amount { get; set; }
    
}