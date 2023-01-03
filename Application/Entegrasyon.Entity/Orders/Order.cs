using Shared.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Orders;

public sealed class Order : BaseEntity
{
    public Guid Id { get; set; }
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int TotalQuantity{ get; set; }//calculated
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal TotalPrice { get; set; }//calculated

    public IEnumerable<OrderItem> OrderItems { get; set; }
    public Address BillingAddress { get; set; }
    public Address ShippingAddress { get; set; }
}