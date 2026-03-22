namespace Entegrasyon.Entity.Dtos.Category;

public record CategoryMarketplaceMappingDto
{
    public int ApplicationCategoryId { get; set; }
    public string ApplicationCategoryName { get; set; } = string.Empty;
    public int MarketPlaceId { get; set; }
    public int MarketPlaceCategoryId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? MarketPlaceCategoryName { get; set; }
}
