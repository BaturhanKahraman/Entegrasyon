namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

/// <summary>
/// Sektör paketi — onboarding sırasında tenant'a önerilen kategori seti.
/// Örn: "Giyim & Moda", "Elektronik", "Kozmetik".
/// </summary>
public class SectorPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>MudBlazor icon adı. Örn: Icons.Material.Filled.Checkroom</summary>
    public string? IconName { get; set; }

    public bool IsActive { get; set; } = true;
    public ICollection<SectorPackageCategory> Categories { get; set; } = [];
}

/// <summary>SectorPackage ↔ MasterCategory bağlantı tablosu (junction).</summary>
public class SectorPackageCategory
{
    public int Id { get; set; }
    public int SectorPackageId { get; set; }
    public SectorPackage SectorPackage { get; set; } = null!;
    public int MasterCategoryId { get; set; }
    public MasterCategory MasterCategory { get; set; } = null!;
}
