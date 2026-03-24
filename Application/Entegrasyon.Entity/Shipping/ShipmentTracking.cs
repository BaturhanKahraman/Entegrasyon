using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Orders;

namespace Entegrasyon.Entity.Shipping;

public sealed class ShipmentTracking : BaseEntity
{
    public long Id { get; set; }
    public Guid? OrderId { get; set; }
    public int CargoCompanyId { get; set; }

    [StringLength(100)]
    public string TrackingNumber { get; set; } = null!;

    public ShipmentStatus CurrentStatus { get; set; }
    public DateTimeOffset? LastStatusUpdate { get; set; }
    public DateTimeOffset? EstimatedDeliveryDate { get; set; }
    public DateTimeOffset? ActualDeliveryDate { get; set; }

    [StringLength(200)]
    public string? RecipientName { get; set; }

    [StringLength(500)]
    public string? RecipientAddress { get; set; }

    // Navigation properties
    public Order? Order { get; set; }
    public CargoCompany CargoCompany { get; set; } = null!;
    public ICollection<ShipmentStatusHistory> StatusHistory { get; set; } = new List<ShipmentStatusHistory>();
}
