namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template markasının bir marketplace'deki karşılığı.
/// </summary>
public sealed class TemplateBrandMarketplaceMapping : TemplateBaseEntity
{
    public int TemplateBrandDataId { get; set; }
    public TemplateBrandData TemplateBrandData { get; set; } = null!;

    public int MarketPlaceId { get; set; }
    public int ExternalBrandId { get; set; }

    /// <summary>
    /// String tipinde harici marka ID'si (Pazarama gibi GUID kullanan marketplace'ler için).
    /// </summary>
    public string? ExternalBrandExternalId { get; set; }
}
