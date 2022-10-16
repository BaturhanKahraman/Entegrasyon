using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Categories;
using Shared.Entity;

namespace Entegrasyon.Entity.Matches;

public sealed class CategoryMarketPlaceMatch 
{
    public int ApplicationCategoryId { get; set; }
    public Category ApplicationCategory { get; set; }
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; }
    public int MarketPlaceCategoryId { get; set; }
}
