namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template paketindeki kargo şirketi tanımı.
/// </summary>
public sealed class TemplateCargoCompanyData : TemplateBaseEntity
{
    public int PackageId { get; set; }
    public MatchedEntityPackage Package { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Code { get; set; }

    // Navigation
    public ICollection<TemplateCargoCompanyMarketplaceMapping> MarketplaceMappings { get; set; } = [];
}
