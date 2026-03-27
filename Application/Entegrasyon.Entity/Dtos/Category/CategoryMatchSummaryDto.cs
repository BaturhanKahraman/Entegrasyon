namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryMatchSummaryDto
{
    public int TotalCategories { get; set; }
    public int MappedCategories { get; set; }
    public int UnmappedCategories { get; set; }
    public List<MarketplaceMatchSummaryItemDto> PerMarketplace { get; set; } = [];
}

public record MarketplaceMatchSummaryItemDto
{
    public int MarketPlaceId { get; set; }
    public string MarketPlaceName { get; set; } = string.Empty;
    public int MappedCount { get; set; }
}
