using Shared.Entity;

namespace Entegrasyon.Entity.Matches;

public class CategoryMarketPlaceMatch : ApplicationEntity
{
    public int ApplicationCategoryId { get; set; }
    public int MarketPlaceId { get; set; }
    public int MarketPlaceCategoryId { get; set; }
}
