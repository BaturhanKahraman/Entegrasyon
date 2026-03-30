namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

/// <summary>
/// Ham marketplace verisi — sync ve delta karşılaştırma için.
/// Master tablolara aktarılmadan önce burada tutulur.
/// </summary>
public class MarketplaceReference : BaseEntity
{
    /// <summary>1=Trendyol, 2=N11, 3=Hepsiburada vb.</summary>
    public int MarketplaceId { get; set; }

    public MarketplaceEntityType EntityType { get; set; }

    /// <summary>Marketplace'deki orijinal ID.</summary>
    public string ExternalId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Üst düğüm ID'si (kategori ağacı için).</summary>
    public string? ParentExternalId { get; set; }

    /// <summary>API'den gelen ham JSON.</summary>
    public string? RawJson { get; set; }

    public DateTimeOffset LastSyncedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum MarketplaceEntityType
{
    Category,
    Attribute,
    Value
}
