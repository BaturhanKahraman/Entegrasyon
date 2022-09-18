using Shared.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entegrasyon.Entity.Orders;

public class Order : LongEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int TotalQuantity{ get;  }//calculated
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal TotalPrice { get;  }//calculated

    public ICollection<OrderItem> OrderItems { get; set; }
    public Address BillingAddress { get; set; }
    public Address ShippingAddress { get; set; }
}