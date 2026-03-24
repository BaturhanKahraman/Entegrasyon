namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template kategorisinin bir marketplace'deki karşılığı.
/// Cross-marketplace paket yapısında her kategori N marketplace mapping'e sahip olabilir.
/// </summary>
public sealed class TemplateCategoryMarketplaceMapping : TemplateBaseEntity
{
    public int TemplateCategoryDataId { get; set; }
    public TemplateCategoryData TemplateCategoryData { get; set; } = null!;

    public int MarketPlaceId { get; set; }
    public string ExternalCategoryId { get; set; } = null!;
    public string? ExternalCategoryName { get; set; }
}
