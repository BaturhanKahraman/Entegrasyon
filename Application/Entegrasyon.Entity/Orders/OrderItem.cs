using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity.Orders;

public class OrderItem:LongEntity
{
    public long OrderId { get; set; }
    public Order Order { get; set; }
    public long ProductId { get; set; }
    public ProductVariant Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
}