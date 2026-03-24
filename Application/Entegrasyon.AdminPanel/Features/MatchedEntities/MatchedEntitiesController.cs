using Entegrasyon.AdminPanel.Infrastructure.Data;
using Entegrasyon.Entity.Templates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.AdminPanel.Features.MatchedEntities;

[Authorize]
public class MatchedEntitiesController(TemplateDbContext templateDb) : Controller
{
    private const string ViewBase = "~/Features/MatchedEntities/Views";

    public async Task<IActionResult> Index(string? search, MatchedEntityType? type)
    {
        var query = templateDb.MatchedEntityPackages.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
            ViewData["Search"] = search;
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.EntityType == type.Value);
            ViewData["TypeFilter"] = type.Value;
        }

        var packages = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PackageListViewModel
            {
                Id = p.Id,
                Name = p.Name,
                EntityType = p.EntityType,
                IsPublished = p.IsPublished,
                Version = p.Version,
                CreatedAt = p.CreatedAt,
                EntityCount = p.EntityType == MatchedEntityType.Category
                    ? p.Categories.Count()
                    : p.EntityType == MatchedEntityType.Brand
                        ? p.Brands.Count()
                        : p.CargoCompanies.Count()
            })
            .ToListAsync();

        return View($"{ViewBase}/Index.cshtml", packages);
    }

    public async Task<IActionResult> Details(int id)
    {
        var package = await templateDb.MatchedEntityPackages
            .Include(p => p.Categories.Where(c => c.ParentTemplateCategoryDataId == null))
                .ThenInclude(c => c.Children)
            .Include(p => p.Categories)
                .ThenInclude(c => c.Attributes)
            .Include(p => p.Categories)
                .ThenInclude(c => c.MarketplaceMappings)
            .Include(p => p.Brands)
                .ThenInclude(b => b.MarketplaceMappings)
            .Include(p => p.CargoCompanies)
                .ThenInclude(cc => cc.MarketplaceMappings)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (package == null)
            return NotFound();

        var vm = new PackageDetailViewModel
        {
            Id = package.Id,
            Name = package.Name,
            Description = package.Description,
            EntityType = package.EntityType,
            IsPublished = package.IsPublished,
            Version = package.Version,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt,
            Categories = package.Categories
                .Where(c => c.ParentTemplateCategoryDataId == null)
                .Select(MapCategory)
                .ToList(),
            Brands = package.Brands.Select(b => new TemplateBrandViewModel
            {
                Id = b.Id,
                Name = b.Name,
                MarketplaceCount = b.MarketplaceMappings.Count
            }).ToList(),
            CargoCompanies = package.CargoCompanies.Select(cc => new TemplateCargoCompanyViewModel
            {
                Id = cc.Id,
                Name = cc.Name,
                Code = cc.Code,
                MarketplaceCount = cc.MarketplaceMappings.Count
            }).ToList()
        };

        return View($"{ViewBase}/Details.cshtml", vm);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View($"{ViewBase}/Create.cshtml", new PackageFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PackageFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View($"{ViewBase}/Create.cshtml", model);

        var package = new MatchedEntityPackage
        {
            Name = model.Name,
            Description = model.Description,
            EntityType = model.EntityType,
            IsPublished = model.IsPublished,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        templateDb.MatchedEntityPackages.Add(package);
        await templateDb.SaveChangesAsync();

        TempData["Success"] = $"'{package.Name}' paketi oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = package.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var package = await templateDb.MatchedEntityPackages.FindAsync(id);
        if (package == null) return NotFound();

        var vm = new PackageFormViewModel
        {
            Id = package.Id,
            Name = package.Name,
            Description = package.Description,
            EntityType = package.EntityType,
            IsPublished = package.IsPublished
        };

        return View($"{ViewBase}/Edit.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PackageFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View($"{ViewBase}/Edit.cshtml", model);

        var package = await templateDb.MatchedEntityPackages.FindAsync(id);
        if (package == null) return NotFound();

        package.Name = model.Name;
        package.Description = model.Description;
        package.EntityType = model.EntityType;
        package.IsPublished = model.IsPublished;
        package.UpdatedAt = DateTimeOffset.UtcNow;

        await templateDb.SaveChangesAsync();

        TempData["Success"] = $"'{package.Name}' paketi güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var package = await templateDb.MatchedEntityPackages.FindAsync(id);
        if (package == null) return NotFound();

        package.IsPublished = !package.IsPublished;
        package.UpdatedAt = DateTimeOffset.UtcNow;
        if (package.IsPublished)
            package.Version++;

        await templateDb.SaveChangesAsync();

        TempData["Success"] = package.IsPublished
            ? $"'{package.Name}' yayınlandı (v{package.Version})."
            : $"'{package.Name}' yayından kaldırıldı.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var package = await templateDb.MatchedEntityPackages.FindAsync(id);
        if (package == null) return NotFound();

        templateDb.MatchedEntityPackages.Remove(package);
        await templateDb.SaveChangesAsync();

        TempData["Success"] = $"'{package.Name}' paketi silindi.";
        return RedirectToAction(nameof(Index));
    }

    private static TemplateCategoryViewModel MapCategory(TemplateCategoryData cat) => new()
    {
        Id = cat.Id,
        Name = cat.Name,
        DefaultVatRate = cat.DefaultVatRate,
        AttributeCount = cat.Attributes.Count,
        MarketplaceCount = cat.MarketplaceMappings.Count,
        Children = cat.Children.Select(MapCategory).ToList()
    };
}
