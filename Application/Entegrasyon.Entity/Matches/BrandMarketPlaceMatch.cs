using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Matches;

public class BrandMarketPlaceMatch
{
    public int ApplicationBrandId { get; set; }
    public Brand ApplicationBrand { get; set; }
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; }
    public int MarketPlaceBrandId { get; set; }
}