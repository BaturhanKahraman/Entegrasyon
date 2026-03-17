using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Matches;

public sealed class CategoryMarketPlaceMatch 
{
    public int ApplicationCategoryId { get; set; }
    public Category ApplicationCategory { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;
    public int MarketPlaceCategoryId { get; set; }
}
