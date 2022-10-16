using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity.Sales;

public sealed class SaleItem:BaseEntity
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public ProductVariant ProductVariant { get; set; }

    public double DiscountPercent { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    
}