namespace Entegrasyon.Entity.Requests;

public record OrderPaginatedRequest() : PaginatedRequest()
{
    public int? MarketPlaceId { get; init; }
    public string? Status { get; init; }
}
