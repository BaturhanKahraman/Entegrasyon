using Entegrasyon.Entity.Shipping;

namespace Entegrasyon.Entity.Dtos.Shipping;

public sealed record ShipmentStatusHistoryDto(
    ShipmentStatus Status,
    string? Description,
    string? Location,
    DateTimeOffset Timestamp);
