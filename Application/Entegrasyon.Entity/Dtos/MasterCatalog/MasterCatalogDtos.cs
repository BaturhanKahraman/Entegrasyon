namespace Entegrasyon.Entity.Dtos.MasterCatalog;

/// <summary>MasterCategory ağaç görünümü için DTO.</summary>
public class MasterCategoryTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public bool IsLeaf { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public List<MasterCategoryTreeDto> Children { get; set; } = [];
}

/// <summary>Sektör paketi görüntüleme DTO'su.</summary>
public class SectorPackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public int CategoryCount { get; set; }
    public List<int> MasterCategoryIds { get; set; } = [];
}
