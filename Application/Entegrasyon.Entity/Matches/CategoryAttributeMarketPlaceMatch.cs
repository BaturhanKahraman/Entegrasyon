using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Matches;

public sealed class CategoryAttributeMarketPlaceMatch
{
    public int ApplicationCategoryAttributeId { get; set; }
    public CategoryAttribute ApplicationCategoryAttribute { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;
    public int MarketPlaceCategoryAttributeId { get; set; }

    /// <summary>
    /// String tipinde harici özellik ID'si (Hepsiburada gibi string ID kullanan marketplace'ler için).
    /// </summary>
    public string? MarketPlaceCategoryAttributeExternalId { get; set; }
}