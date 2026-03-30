namespace Entegrasyon.Entity.Dtos.Branches;

public record BranchOfficePageListDto(
    int Id,
    string Name,
    int UserCount,
    int TotalStock,
    int MarketPlaceCount,
    DateTimeOffset CreatedAt,
    bool IsDefaultMarketPlaceStock)
{
    // Client-side populated after query
    public List<string> MarketPlaceNames { get; init; } = [];
}
