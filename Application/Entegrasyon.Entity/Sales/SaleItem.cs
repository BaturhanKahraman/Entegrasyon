using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity.Sales;

public class SaleItem:LongEntity
{
    public long ProductId { get; set; }
    public ProductVariant ProductVariant { get; set; }

    public double DiscountPercent { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    
}