namespace Entegrasyon.AdminPanel.Features.MasterCatalog;

// ── Shared ──────────────────────────────────────────────────────────────

public class MarketplaceMappingVm
{
    public int MarketplaceId { get; set; }
    public string MarketplaceName { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string ExternalName { get; set; } = string.Empty;
}

// ── Categories ──────────────────────────────────────────────────────────

public class CategoryBreadcrumbVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CategoryTreeItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ChildCount { get; set; }
    public bool IsLeaf { get; set; }
    public bool IsActive { get; set; }
    public int MappingCount { get; set; }
    public int AttributeCount { get; set; }
}

public class CategoriesIndexVm
{
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public List<CategoryBreadcrumbVm> Breadcrumbs { get; set; } = [];
    public List<CategoryTreeItemVm> Categories { get; set; } = [];
    public string? Search { get; set; }
    public int TotalCount { get; set; }
    public int LeafCount { get; set; }
    public int MappedCount { get; set; }

    /// <summary>Arama sonuçları için: tam yol göster.</summary>
    public Dictionary<int, string> FullPaths { get; set; } = [];
}

public class CategoryAttributeVm
{
    public int AttributeId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string HumanizedName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
    public int ValueCount { get; set; }
    public bool AllowCustom { get; set; }
}

public class CategoryDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLeaf { get; set; }
    public bool IsActive { get; set; }
    public string? OriginalMarketplaceName { get; set; }
    public string? OriginalExternalId { get; set; }
    public List<CategoryBreadcrumbVm> Breadcrumbs { get; set; } = [];
    public List<CategoryAttributeVm> Attributes { get; set; } = [];
    public List<MarketplaceMappingVm> MarketplaceMappings { get; set; } = [];
    public List<CategoryTreeItemVm> Children { get; set; } = [];
}

// ── Brands ──────────────────────────────────────────────────────────────

public class BrandListVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<MarketplaceMappingVm> Mappings { get; set; } = [];
}

public class BrandsIndexVm
{
    public List<BrandListVm> Brands { get; set; } = [];
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public int MappedCount { get; set; }
}

// ── Attributes ──────────────────────────────────────────────────────────

public class AttributeListVm
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string HumanizedName { get; set; } = string.Empty;
    public bool AllowCustom { get; set; }
    public bool IsActive { get; set; }
    public int ValueCount { get; set; }
    public int CategoryCount { get; set; }
}

public class AttributesIndexVm
{
    public List<AttributeListVm> Attributes { get; set; } = [];
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class AttributeValueVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<MarketplaceMappingVm> Mappings { get; set; } = [];
}

public class AttributeDetailsVm
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string HumanizedName { get; set; } = string.Empty;
    public bool AllowCustom { get; set; }
    public bool IsActive { get; set; }
    public List<AttributeValueVm> Values { get; set; } = [];
    public List<MarketplaceMappingVm> MarketplaceMappings { get; set; } = [];
    public List<CategoryBreadcrumbVm> LinkedCategories { get; set; } = [];
}
