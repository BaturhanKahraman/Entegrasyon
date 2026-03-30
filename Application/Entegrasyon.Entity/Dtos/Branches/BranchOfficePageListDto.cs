namespace Entegrasyon.Entity.Dtos.Branches;

public record BranchOfficePageListDto(
    int Id,
    string Name,
    int UserCount,
    int TotalStock,
    IEnumerable<string> MarketPlaceNames,
    DateTimeOffset CreatedAt,
    bool IsDefaultMarketPlaceStock);
