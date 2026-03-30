namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

public class MasterCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public MasterCategory? Parent { get; set; }
    public ICollection<MasterCategory> Children { get; set; } = [];
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Yaprak düğüm — ürün bu kategoriye atanabilir.</summary>
    public bool IsLeaf { get; set; }

    /// <summary>Bu kategori hangi marketplace'den geldi (1=Trendyol, vb.)</summary>
    public int? OriginalMarketplaceId { get; set; }

    /// <summary>Kaynak marketplace'deki harici ID.</summary>
    public string? OriginalExternalId { get; set; }

    public ICollection<MasterCategoryAttribute> CategoryAttributes { get; set; } = [];
    public ICollection<MasterCategoryMarketplaceMapping> MarketplaceMappings { get; set; } = [];
    public ICollection<SectorPackageCategory> SectorPackageCategories { get; set; } = [];
}
