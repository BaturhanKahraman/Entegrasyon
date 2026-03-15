namespace Entegrasyon.Entity.Dtos.Category;

public record CreateCategoryMarketplaceMatchDto
{
    public int ApplicationCategoryId { get; set; }
    public int MarketPlaceId { get; set; }
    public int MarketPlaceCategoryId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? MarketPlaceCategoryName { get; set; }
}
