using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Shipping;

public sealed class ShipmentStatusHistory : BaseEntity
{
    public long Id { get; set; }
    public long ShipmentTrackingId { get; set; }
    public ShipmentStatus Status { get; set; }

    [StringLength(500)]
    public string? StatusDescription { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    // Navigation property
    public ShipmentTracking ShipmentTracking { get; set; } = null!;
}
