using Entegrasyon.Entity.Shipping;

namespace Entegrasyon.Entity.Dtos.Shipping;

public sealed record ShipmentTrackingDto(
    long Id,
    Guid? OrderId,
    int CargoCompanyId,
    string CargoCompanyName,
    string TrackingNumber,
    ShipmentStatus CurrentStatus,
    DateTimeOffset? LastStatusUpdate,
    DateTimeOffset? EstimatedDeliveryDate,
    DateTimeOffset? ActualDeliveryDate,
    string? RecipientName,
    string? RecipientAddress,
    List<ShipmentStatusHistoryDto> StatusHistories);
