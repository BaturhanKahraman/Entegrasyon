using System.ComponentModel.DataAnnotations.Schema;
using Entegrasyon.Entity.Products;
using Shared.Entity;

namespace Entegrasyon.Entity.Orders;

public sealed class OrderItem : BaseEntity
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; }
    public Guid? ProductId { get; set; }
    public ProductVariant Product { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitPrice { get; set; }
    
}