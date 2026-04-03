using Entegrasyon.Entity.Shipping;

namespace Entegrasyon.Entity.Requests;

public record ShipmentPaginatedRequest() : PaginatedRequest()
{
    public ShipmentStatus? Status { get; init; }
    public int? CargoCompanyId { get; init; }
}
