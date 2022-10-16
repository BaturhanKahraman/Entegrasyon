namespace Entegrasyon.Entity.Categories;

public sealed class CategoryMarketplace
{
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; }
    public int MarketPlaceCategoryId { get; set; }
}