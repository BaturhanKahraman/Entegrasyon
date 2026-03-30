namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

/// <summary>MasterCategory ↔ Marketplace harici kategori eşleştirmesi.</summary>
public class MasterCategoryMarketplaceMapping
{
    public int Id { get; set; }
    public int MasterCategoryId { get; set; }
    public MasterCategory MasterCategory { get; set; } = null!;

    /// <summary>1=Trendyol, 2=N11, 3=Hepsiburada vb.</summary>
    public int MarketplaceId { get; set; }

    public string ExternalCategoryId { get; set; } = string.Empty;
    public string ExternalCategoryName { get; set; } = string.Empty;
}

/// <summary>MasterAttribute ↔ Marketplace harici attribute eşleştirmesi.</summary>
public class MasterAttributeMarketplaceMapping
{
    public int Id { get; set; }
    public int MasterAttributeId { get; set; }
    public MasterAttribute MasterAttribute { get; set; } = null!;
    public int MarketplaceId { get; set; }
    public string ExternalAttributeId { get; set; } = string.Empty;
    public string ExternalAttributeName { get; set; } = string.Empty;
}

/// <summary>MasterAttributeValue ↔ Marketplace harici değer eşleştirmesi.</summary>
public class MasterValueMarketplaceMapping
{
    public int Id { get; set; }
    public int MasterAttributeValueId { get; set; }
    public MasterAttributeValue MasterAttributeValue { get; set; } = null!;
    public int MarketplaceId { get; set; }
    public string ExternalValueId { get; set; } = string.Empty;
    public string ExternalValueName { get; set; } = string.Empty;
}
