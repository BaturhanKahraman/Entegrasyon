namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

public class MasterAttribute : BaseEntity
{
    /// <summary>Makine-okuyabilir anahtar (örn: "Renk", "Beden").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Kullanıcıya gösterilen isim.</summary>
    public string HumanizedName { get; set; } = string.Empty;

    /// <summary>Özel değer girişine izin verilir mi?</summary>
    public bool AllowCustom { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MasterAttributeValue> Values { get; set; } = [];
    public ICollection<MasterCategoryAttribute> CategoryLinks { get; set; } = [];
    public ICollection<MasterAttributeMarketplaceMapping> MarketplaceMappings { get; set; } = [];
}

public class MasterAttributeValue : BaseEntity
{
    public int MasterAttributeId { get; set; }
    public MasterAttribute MasterAttribute { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<MasterValueMarketplaceMapping> MarketplaceMappings { get; set; } = [];
}

/// <summary>Kategori-Attribute bağlantı tablosu (junction).</summary>
public class MasterCategoryAttribute
{
    public int Id { get; set; }
    public int MasterCategoryId { get; set; }
    public MasterCategory MasterCategory { get; set; } = null!;
    public int MasterAttributeId { get; set; }
    public MasterAttribute MasterAttribute { get; set; } = null!;

    public bool IsRequired { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
}
