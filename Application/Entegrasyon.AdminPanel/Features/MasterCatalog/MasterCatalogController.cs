using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.MasterCatalog;

[Authorize]
public class MasterCatalogController(AdminPanelDbContext dbContext) : Controller
{
    private const string ViewBase = "~/Features/MasterCatalog/Views";

    private static readonly Dictionary<int, string> MarketplaceNames = new()
    {
        [1] = "Trendyol",
        [2] = "N11",
        [3] = "Hepsiburada",
        [4] = "Amazon",
        [5] = "Pazarama",
        [7] = "PttAVM",
        [8] = "Çiçeksepeti"
    };

    private static string GetMarketplaceName(int id) =>
        MarketplaceNames.GetValueOrDefault(id, $"MP-{id}");

    // ── Categories ──────────────────────────────────────────────────────

    public async Task<IActionResult> Categories(int? parentId, string? search)
    {
        var totalCount = await dbContext.MasterCategories.CountAsync();
        var leafCount = await dbContext.MasterCategories.CountAsync(c => c.IsLeaf);
        var mappedCount = await dbContext.MasterCategoryMarketplaceMappings
            .Select(m => m.MasterCategoryId).Distinct().CountAsync();

        // Arama modu
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            var results = await dbContext.MasterCategories
                .Where(c => c.Name.ToLower().Contains(term))
                .OrderBy(c => c.Name)
                .Take(100)
                .Select(c => new CategoryTreeItemVm
                {
                    Id = c.Id,
                    Name = c.Name,
                    ChildCount = c.Children.Count,
                    IsLeaf = c.IsLeaf,
                    IsActive = c.IsActive,
                    MappingCount = c.MarketplaceMappings.Count,
                    AttributeCount = c.CategoryAttributes.Count
                })
                .ToListAsync();

            // Tam yolları hesapla
            var fullPaths = new Dictionary<int, string>();
            foreach (var cat in results)
            {
                fullPaths[cat.Id] = await BuildCategoryPathAsync(cat.Id);
            }

            var vm = new CategoriesIndexVm
            {
                Search = search,
                Categories = results,
                TotalCount = totalCount,
                LeafCount = leafCount,
                MappedCount = mappedCount,
                FullPaths = fullPaths
            };
            return View($"{ViewBase}/Categories/Index.cshtml", vm);
        }

        // Drill-down modu
        var query = dbContext.MasterCategories
            .Where(c => c.ParentId == parentId);

        var categories = await query
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryTreeItemVm
            {
                Id = c.Id,
                Name = c.Name,
                ChildCount = c.Children.Count,
                IsLeaf = c.IsLeaf,
                IsActive = c.IsActive,
                MappingCount = c.MarketplaceMappings.Count,
                AttributeCount = c.CategoryAttributes.Count
            })
            .ToListAsync();

        var breadcrumbs = parentId.HasValue
            ? await BuildBreadcrumbsAsync(parentId.Value)
            : [];

        string? parentName = null;
        if (parentId.HasValue)
        {
            parentName = await dbContext.MasterCategories
                .Where(c => c.Id == parentId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        var model = new CategoriesIndexVm
        {
            ParentId = parentId,
            ParentName = parentName,
            Breadcrumbs = breadcrumbs,
            Categories = categories,
            TotalCount = totalCount,
            LeafCount = leafCount,
            MappedCount = mappedCount
        };

        return View($"{ViewBase}/Categories/Index.cshtml", model);
    }

    public async Task<IActionResult> CategoryDetails(int id)
    {
        var category = await dbContext.MasterCategories
            .Include(c => c.MarketplaceMappings)
            .Include(c => c.CategoryAttributes)
                .ThenInclude(ca => ca.MasterAttribute)
                    .ThenInclude(a => a.Values)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return NotFound();

        var breadcrumbs = await BuildBreadcrumbsAsync(id);

        var vm = new CategoryDetailsVm
        {
            Id = category.Id,
            Name = category.Name,
            IsLeaf = category.IsLeaf,
            IsActive = category.IsActive,
            OriginalMarketplaceName = category.OriginalMarketplaceId.HasValue
                ? GetMarketplaceName(category.OriginalMarketplaceId.Value)
                : null,
            OriginalExternalId = category.OriginalExternalId,
            Breadcrumbs = breadcrumbs,
            Attributes = category.CategoryAttributes
                .Select(ca => new CategoryAttributeVm
                {
                    AttributeId = ca.MasterAttributeId,
                    Key = ca.MasterAttribute.Key,
                    HumanizedName = ca.MasterAttribute.HumanizedName,
                    IsRequired = ca.IsRequired,
                    IsVarianter = ca.IsVarianter,
                    IsSlicer = ca.IsSlicer,
                    ValueCount = ca.MasterAttribute.Values.Count,
                    AllowCustom = ca.MasterAttribute.AllowCustom
                })
                .OrderByDescending(a => a.IsRequired)
                .ThenBy(a => a.HumanizedName)
                .ToList(),
            MarketplaceMappings = category.MarketplaceMappings
                .Select(m => new MarketplaceMappingVm
                {
                    MarketplaceId = m.MarketplaceId,
                    MarketplaceName = GetMarketplaceName(m.MarketplaceId),
                    ExternalId = m.ExternalCategoryId,
                    ExternalName = m.ExternalCategoryName
                })
                .ToList(),
            Children = category.Children
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new CategoryTreeItemVm
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsLeaf = c.IsLeaf,
                    IsActive = c.IsActive
                })
                .ToList()
        };

        return View($"{ViewBase}/Categories/Details.cshtml", vm);
    }

    // ── Brands ──────────────────────────────────────────────────────────

    public async Task<IActionResult> Brands(string? search, int page = 1)
    {
        const int pageSize = 50;
        var query = dbContext.MasterBrands.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();
        var mappedCount = await dbContext.MasterBrandMarketplaceMappings
            .Select(m => m.MasterBrandId).Distinct().CountAsync();

        var brands = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BrandListVm
            {
                Id = b.Id,
                Name = b.Name,
                IsActive = b.IsActive,
                Mappings = b.MarketplaceMappings
                    .Select(m => new MarketplaceMappingVm
                    {
                        MarketplaceId = m.MarketplaceId,
                        MarketplaceName = GetMarketplaceName(m.MarketplaceId),
                        ExternalId = m.ExternalBrandId.ToString(),
                        ExternalName = m.ExternalBrandName ?? ""
                    })
                    .ToList()
            })
            .ToListAsync();

        var vm = new BrandsIndexVm
        {
            Brands = brands,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            MappedCount = mappedCount
        };

        return View($"{ViewBase}/Brands/Index.cshtml", vm);
    }

    // ── Attributes ──────────────────────────────────────────────────────

    public async Task<IActionResult> Attributes(string? search, int page = 1)
    {
        const int pageSize = 50;
        var query = dbContext.MasterAttributes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                a.Key.ToLower().Contains(term) ||
                a.HumanizedName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var attributes = await query
            .OrderBy(a => a.HumanizedName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AttributeListVm
            {
                Id = a.Id,
                Key = a.Key,
                HumanizedName = a.HumanizedName,
                AllowCustom = a.AllowCustom,
                IsActive = a.IsActive,
                ValueCount = a.Values.Count,
                CategoryCount = a.CategoryLinks.Count
            })
            .ToListAsync();

        var vm = new AttributesIndexVm
        {
            Attributes = attributes,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View($"{ViewBase}/Attributes/Index.cshtml", vm);
    }

    public async Task<IActionResult> AttributeDetails(int id)
    {
        var attribute = await dbContext.MasterAttributes
            .Include(a => a.Values)
                .ThenInclude(v => v.MarketplaceMappings)
            .Include(a => a.MarketplaceMappings)
            .Include(a => a.CategoryLinks)
                .ThenInclude(cl => cl.MasterCategory)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attribute is null)
            return NotFound();

        var vm = new AttributeDetailsVm
        {
            Id = attribute.Id,
            Key = attribute.Key,
            HumanizedName = attribute.HumanizedName,
            AllowCustom = attribute.AllowCustom,
            IsActive = attribute.IsActive,
            Values = attribute.Values
                .OrderBy(v => v.Name)
                .Select(v => new AttributeValueVm
                {
                    Id = v.Id,
                    Name = v.Name,
                    IsActive = v.IsActive,
                    Mappings = v.MarketplaceMappings
                        .Select(m => new MarketplaceMappingVm
                        {
                            MarketplaceId = m.MarketplaceId,
                            MarketplaceName = GetMarketplaceName(m.MarketplaceId),
                            ExternalId = m.ExternalValueId,
                            ExternalName = m.ExternalValueName
                        })
                        .ToList()
                })
                .ToList(),
            MarketplaceMappings = attribute.MarketplaceMappings
                .Select(m => new MarketplaceMappingVm
                {
                    MarketplaceId = m.MarketplaceId,
                    MarketplaceName = GetMarketplaceName(m.MarketplaceId),
                    ExternalId = m.ExternalAttributeId,
                    ExternalName = m.ExternalAttributeName
                })
                .ToList(),
            LinkedCategories = attribute.CategoryLinks
                .Select(cl => new CategoryBreadcrumbVm
                {
                    Id = cl.MasterCategoryId,
                    Name = cl.MasterCategory.Name
                })
                .OrderBy(c => c.Name)
                .ToList()
        };

        return View($"{ViewBase}/Attributes/Details.cshtml", vm);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<List<CategoryBreadcrumbVm>> BuildBreadcrumbsAsync(int categoryId)
    {
        var breadcrumbs = new List<CategoryBreadcrumbVm>();
        int? currentId = categoryId;

        while (currentId.HasValue)
        {
            var cat = await dbContext.MasterCategories
                .Where(c => c.Id == currentId.Value)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .FirstOrDefaultAsync();

            if (cat is null) break;

            breadcrumbs.Insert(0, new CategoryBreadcrumbVm { Id = cat.Id, Name = cat.Name });
            currentId = cat.ParentId;
        }

        return breadcrumbs;
    }

    private async Task<string> BuildCategoryPathAsync(int categoryId)
    {
        var parts = new List<string>();
        int? currentId = categoryId;

        while (currentId.HasValue)
        {
            var cat = await dbContext.MasterCategories
                .Where(c => c.Id == currentId.Value)
                .Select(c => new { c.Name, c.ParentId })
                .FirstOrDefaultAsync();

            if (cat is null) break;

            parts.Insert(0, cat.Name);
            currentId = cat.ParentId;
        }

        return string.Join(" > ", parts);
    }
}
