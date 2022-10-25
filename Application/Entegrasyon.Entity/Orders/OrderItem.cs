using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity.Orders;

public sealed class OrderItem : BaseEntity
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    public Guid ProductId { get; set; }
    public ProductVariant Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
}