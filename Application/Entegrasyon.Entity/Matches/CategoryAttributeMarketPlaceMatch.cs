using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Matches;

public sealed class CategoryAttributeMarketPlaceMatch
{
    public int ApplicationCategoryAttributeId { get; set; }
    public CategoryAttribute ApplicationCategoryAttribute { get; set; }
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; }
    public int MarketPlaceCategoryAttributeId { get; set; }
}