using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.Licenses;

[Authorize]
public class LicensesController(AdminPanelDbContext dbContext) : Controller
{
    private const string ViewBase = "~/Features/Licenses/Views";

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        var licenses = await dbContext.TenantLicenses
            .Include(l => l.Tenant)
            .OrderByDescending(l => l.StartDate)
            .Select(l => new LicenseListItemViewModel
            {
                Id = l.Id,
                TenantId = l.TenantId,
                CompanyName = l.Tenant.CompanyName,
                Type = l.Type,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Notes = l.Notes
            })
            .ToListAsync();

        var model = new LicenseListViewModel
        {
            ActiveLicenses = licenses.Where(l => l.Status == "Aktif").ToList(),
            ExpiredLicenses = licenses.Where(l => l.Status == "Süresi Dolmuş").ToList(),
            UpcomingLicenses = licenses.Where(l => l.Status == "Yaklaşan").ToList()
        };

        return View($"{ViewBase}/Index.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? tenantId)
    {
        var model = new LicenseFormViewModel
        {
            TenantId = tenantId ?? 0,
            Tenants = await GetTenantDropdownItems()
        };

        return View($"{ViewBase}/Create.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LicenseFormViewModel model)
    {
        if (model.EndDate <= model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");

        if (!await dbContext.Tenants.AnyAsync(t => t.Id == model.TenantId))
            ModelState.AddModelError(nameof(model.TenantId), "Seçilen firma bulunamadı.");

        if (!ModelState.IsValid)
        {
            model.Tenants = await GetTenantDropdownItems();
            return View($"{ViewBase}/Create.cshtml", model);
        }

        var license = new TenantLicense
        {
            TenantId = model.TenantId,
            Type = model.Type,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Notes = model.Notes
        };

        dbContext.TenantLicenses.Add(license);
        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Lisans başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var license = await dbContext.TenantLicenses.FindAsync(id);
        if (license is null)
            return NotFound();

        var model = new LicenseFormViewModel
        {
            Id = license.Id,
            TenantId = license.TenantId,
            Type = license.Type,
            StartDate = license.StartDate,
            EndDate = license.EndDate,
            Notes = license.Notes,
            Tenants = await GetTenantDropdownItems()
        };

        return View($"{ViewBase}/Edit.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LicenseFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (model.EndDate <= model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");

        if (!await dbContext.Tenants.AnyAsync(t => t.Id == model.TenantId))
            ModelState.AddModelError(nameof(model.TenantId), "Seçilen firma bulunamadı.");

        if (!ModelState.IsValid)
        {
            model.Tenants = await GetTenantDropdownItems();
            return View($"{ViewBase}/Edit.cshtml", model);
        }

        var license = await dbContext.TenantLicenses.FindAsync(id);
        if (license is null)
            return NotFound();

        license.TenantId = model.TenantId;
        license.Type = model.Type;
        license.StartDate = model.StartDate;
        license.EndDate = model.EndDate;
        license.Notes = model.Notes;
        license.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Lisans bilgileri güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var license = await dbContext.TenantLicenses
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (license is null)
            return NotFound();

        var companyName = license.Tenant.CompanyName;
        dbContext.TenantLicenses.Remove(license);
        await dbContext.SaveChangesAsync();

        TempData["Success"] = $"\"{companyName}\" firmasının lisansı silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<TenantDropdownItem>> GetTenantDropdownItems()
    {
        return await dbContext.Tenants
            .Where(t => t.IsActive)
            .OrderBy(t => t.CompanyName)
            .Select(t => new TenantDropdownItem
            {
                Id = t.Id,
                CompanyName = t.CompanyName
            })
            .ToListAsync();
    }
}
