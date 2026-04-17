using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Sales;

public class SaleReturnItem : BaseEntity
{
    public long Id { get; set; }
    public long SaleReturnId { get; set; }
    public SaleReturn SaleReturn { get; set; } = null!;

    public Guid? SaleItemId { get; set; }
    public SaleItem? SaleItem { get; set; }

    public long? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public int Quantity { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public bool RestoredToStock { get; set; }
    public DateTimeOffset? RestoredAt { get; set; }
    public Guid? RestoredByUserId { get; set; }
    public ApplicationUser? RestoredBy { get; set; }
}
