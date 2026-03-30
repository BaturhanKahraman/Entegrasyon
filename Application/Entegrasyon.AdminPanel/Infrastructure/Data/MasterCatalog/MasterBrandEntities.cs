namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

/// <summary>Master marka tanımı — tüm marketplace'lerden normalize edilmiş marka.</summary>
public class MasterBrand
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MasterBrandMarketplaceMapping> MarketplaceMappings { get; set; } = [];
}

/// <summary>MasterBrand ↔ Marketplace harici marka eşleştirmesi.</summary>
public class MasterBrandMarketplaceMapping
{
    public int Id { get; set; }
    public int MasterBrandId { get; set; }
    public MasterBrand MasterBrand { get; set; } = null!;

    /// <summary>1=Trendyol, 2=N11, 3=Hepsiburada vb.</summary>
    public int MarketplaceId { get; set; }

    public int ExternalBrandId { get; set; }
    public string? ExternalBrandName { get; set; }
}
