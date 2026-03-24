namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template kargo şirketinin bir marketplace'deki karşılığı.
/// </summary>
public sealed class TemplateCargoCompanyMarketplaceMapping : TemplateBaseEntity
{
    public int TemplateCargoCompanyDataId { get; set; }
    public TemplateCargoCompanyData TemplateCargoCompanyData { get; set; } = null!;

    public int MarketPlaceId { get; set; }
    public int ExternalCargoCompanyId { get; set; }
}
