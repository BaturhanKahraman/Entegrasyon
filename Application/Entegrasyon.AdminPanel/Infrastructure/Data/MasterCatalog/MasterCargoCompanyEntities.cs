namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

public class MasterCargoCompany
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MasterCargoCompanyMarketplaceMapping> MarketplaceMappings { get; set; } = [];
}

public class MasterCargoCompanyMarketplaceMapping
{
    public int Id { get; set; }
    public int MasterCargoCompanyId { get; set; }
    public MasterCargoCompany MasterCargoCompany { get; set; } = null!;
    public int MarketplaceId { get; set; }
    public int ExternalCargoCompanyId { get; set; }
    public string? ExternalCargoCompanyName { get; set; }
}
