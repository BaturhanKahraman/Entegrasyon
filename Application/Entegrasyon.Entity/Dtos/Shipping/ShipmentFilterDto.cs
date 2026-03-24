using Entegrasyon.Entity.Shipping;

namespace Entegrasyon.Entity.Dtos.Shipping;

public sealed record ShipmentFilterDto(
    ShipmentStatus? Status = null,
    int? CargoCompanyId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
