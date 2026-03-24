namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template paketindeki marka tanımı.
/// </summary>
public sealed class TemplateBrandData : TemplateBaseEntity
{
    public int PackageId { get; set; }
    public MatchedEntityPackage Package { get; set; } = null!;

    public string Name { get; set; } = null!;

    // Navigation
    public ICollection<TemplateBrandMarketplaceMapping> MarketplaceMappings { get; set; } = [];
}
