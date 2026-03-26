using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.Tenants;

[Authorize]
public class TenantsController(
    AdminPanelDbContext dbContext,
    TenantProvisioningService provisioningService) : Controller
{
    private const string ViewBase = "~/Features/Tenants/Views";

    public async Task<IActionResult> Index(string? search)
    {
        var query = dbContext.Tenants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t =>
                t.CompanyName.ToLower().Contains(term) ||
                t.Subdomain.ToLower().Contains(term));
            ViewData["Search"] = search;
        }

        var tenants = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TenantListViewModel
            {
                Id = t.Id,
                CompanyName = t.CompanyName,
                Subdomain = t.Subdomain,
                DatabaseType = t.DatabaseType,
                IsActive = t.IsActive,
                UserCount = t.UserCount,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return View($"{ViewBase}/Index.cshtml", tenants);
    }

    public async Task<IActionResult> Details(int id)
    {
        var tenant = await dbContext.Tenants
            .Include(t => t.Licenses)
            .Include(t => t.ApplicationLogs)
            .Include(t => t.AiCreditAccount)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant is null)
            return NotFound();

        var vm = new TenantDetailViewModel
        {
            Id = tenant.Id,
            CompanyName = tenant.CompanyName,
            Subdomain = tenant.Subdomain,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone,
            Address = tenant.Address,
            TaxNumber = tenant.TaxNumber,
            ConnectionString = tenant.ConnectionString,
            DatabaseType = tenant.DatabaseType,
            IsActive = tenant.IsActive,
            UserCount = tenant.UserCount,
            Notes = tenant.Notes,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt,
            TotalLicenseCount = tenant.Licenses.Count,
            ActiveLicenseCount = tenant.Licenses.Count(l => l.IsActive),
            CurrentLicenseType = tenant.Licenses
                .Where(l => l.IsActive)
                .OrderByDescending(l => l.EndDate)
                .Select(l => l.Type.ToString())
                .FirstOrDefault(),
            LogCount = tenant.ApplicationLogs.Count,
            ErrorLogCount = tenant.ApplicationLogs.Count(l => l.Level is "Error" or "Critical"),
            ImageCredits = tenant.AiCreditAccount?.ImageGenerationCredits ?? 0,
            DescriptionCredits = tenant.AiCreditAccount?.ProductDescriptionCredits ?? 0
        };

        return View($"{ViewBase}/Details.cshtml", vm);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View($"{ViewBase}/Create.cshtml", new TenantFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenantFormViewModel model)
    {
        if (await dbContext.Tenants.AnyAsync(t => t.Subdomain == model.Subdomain))
            ModelState.AddModelError(nameof(model.Subdomain), "Bu subdomain zaten kullanılıyor.");

        if (!ModelState.IsValid)
            return View($"{ViewBase}/Create.cshtml", model);

        var tenant = new Tenant
        {
            CompanyName = model.CompanyName,
            Subdomain = model.Subdomain,
            ContactEmail = model.ContactEmail,
            ContactPhone = model.ContactPhone,
            Address = model.Address,
            TaxNumber = model.TaxNumber,
            ConnectionString = model.ConnectionString,
            DatabaseType = model.DatabaseType,
            Notes = model.Notes
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var provisionResult = await provisioningService.ProvisionAsync(tenant.ConnectionString);
        if (!provisionResult.Success)
        {
            ModelState.AddModelError("", $"Veritabani olusturulamadi: {provisionResult.ErrorMessage}");
            dbContext.Tenants.Remove(tenant);
            await dbContext.SaveChangesAsync();
            return View($"{ViewBase}/Create.cshtml", model);
        }

        TempData["Success"] = "Firma başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tenant = await dbContext.Tenants.FindAsync(id);
        if (tenant is null)
            return NotFound();

        var vm = new TenantFormViewModel
        {
            Id = tenant.Id,
            CompanyName = tenant.CompanyName,
            Subdomain = tenant.Subdomain,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone,
            Address = tenant.Address,
            TaxNumber = tenant.TaxNumber,
            ConnectionString = tenant.ConnectionString,
            DatabaseType = tenant.DatabaseType,
            Notes = tenant.Notes
        };

        return View($"{ViewBase}/Edit.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TenantFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (await dbContext.Tenants.AnyAsync(t => t.Subdomain == model.Subdomain && t.Id != id))
            ModelState.AddModelError(nameof(model.Subdomain), "Bu subdomain zaten kullanılıyor.");

        if (!ModelState.IsValid)
            return View($"{ViewBase}/Edit.cshtml", model);

        var tenant = await dbContext.Tenants.FindAsync(id);
        if (tenant is null)
            return NotFound();

        tenant.CompanyName = model.CompanyName;
        tenant.Subdomain = model.Subdomain;
        tenant.ContactEmail = model.ContactEmail;
        tenant.ContactPhone = model.ContactPhone;
        tenant.Address = model.Address;
        tenant.TaxNumber = model.TaxNumber;
        tenant.ConnectionString = model.ConnectionString;
        tenant.DatabaseType = model.DatabaseType;
        tenant.Notes = model.Notes;
        tenant.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        TempData["Success"] = "Firma bilgileri güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tenant = await dbContext.Tenants.FindAsync(id);
        if (tenant is null)
            return NotFound();

        tenant.IsActive = false;
        tenant.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        TempData["Success"] = $"\"{tenant.CompanyName}\" pasif duruma alındı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var tenant = await dbContext.Tenants.FindAsync(id);
        if (tenant is null)
            return NotFound();

        tenant.IsActive = !tenant.IsActive;
        tenant.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var status = tenant.IsActive ? "aktif" : "pasif";
        TempData["Success"] = $"\"{tenant.CompanyName}\" {status} duruma alındı.";
        return RedirectToAction(nameof(Index));
    }
}
