namespace Entegrasyon.Entity.Dtos.Shipping;

public sealed record CargoSummaryDto(
    int TotalShipments,
    int InTransitCount,
    int DeliveredCount,
    int FailedCount,
    int PendingCount);
