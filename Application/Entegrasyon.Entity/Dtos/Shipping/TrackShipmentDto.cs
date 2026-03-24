namespace Entegrasyon.Entity.Dtos.Shipping;

public sealed record TrackShipmentDto(
    string TrackingNumber,
    int CargoCompanyId);
