namespace Entegrasyon.Entity.Dtos.Category;

public record CreateAttributeMarketPlaceMatchDto
{
    public int ApplicationCategoryAttributeId { get; set; }
    public int MarketPlaceId { get; set; }
    public int MarketPlaceCategoryAttributeId { get; set; }
}
