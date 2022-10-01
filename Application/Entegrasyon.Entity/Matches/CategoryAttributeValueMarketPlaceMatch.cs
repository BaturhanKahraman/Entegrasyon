using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Matches;

public class CategoryAttributeValueMarketPlaceMatch
{
    public int ApplicationCategoryAttributeValueId { get; set; }
    public CategoryAttributeValue ApplicationCategoryAttributeValue { get; set; }
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; }
    public int MarketPlaceCategoryAttributeValueId { get; set; }
}