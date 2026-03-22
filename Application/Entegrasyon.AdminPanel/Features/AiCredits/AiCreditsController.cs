using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.AiCredits;

[Authorize]
public class AiCreditsController(AdminPanelDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var tenants = await dbContext.Tenants
            .Include(t => t.AiCreditAccount)
            .OrderBy(t => t.CompanyName)
            .Select(t => new AiCreditListItemViewModel
            {
                TenantId = t.Id,
                CompanyName = t.CompanyName,
                Subdomain = t.Subdomain,
                IsActive = t.IsActive,
                HasAccount = t.AiCreditAccount != null,
                ImageGenerationCredits = t.AiCreditAccount != null ? t.AiCreditAccount.ImageGenerationCredits : 0,
                ProductDescriptionCredits = t.AiCreditAccount != null ? t.AiCreditAccount.ProductDescriptionCredits : 0
            })
            .ToListAsync();

        var model = new AiCreditListViewModel { Items = tenants };
        return View("~/Features/AiCredits/Views/Index.cshtml", model);
    }

    public async Task<IActionResult> Details(int tenantId)
    {
        var tenant = await dbContext.Tenants
            .Include(t => t.AiCreditAccount)
                .ThenInclude(a => a!.Transactions)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant is null)
            return NotFound();

        var model = new AiCreditDetailViewModel
        {
            TenantId = tenant.Id,
            CompanyName = tenant.CompanyName,
            ImageGenerationCredits = tenant.AiCreditAccount?.ImageGenerationCredits ?? 0,
            ProductDescriptionCredits = tenant.AiCreditAccount?.ProductDescriptionCredits ?? 0,
            Transactions = tenant.AiCreditAccount?.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new AiCreditTransactionViewModel
                {
                    Id = t.Id,
                    CreditType = t.CreditType == AiCreditType.ImageGeneration ? "Resim Olusturma" : "Urun Aciklamasi",
                    Amount = t.Amount,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                })
                .ToList() ?? []
        };

        return View("~/Features/AiCredits/Views/Details.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> AddCredits(int tenantId)
    {
        var tenant = await dbContext.Tenants.FindAsync(tenantId);
        if (tenant is null)
            return NotFound();

        var model = new AddCreditsFormViewModel
        {
            TenantId = tenant.Id,
            CompanyName = tenant.CompanyName
        };

        return View("~/Features/AiCredits/Views/AddCredits.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCredits(AddCreditsFormViewModel model)
    {
        var tenant = await dbContext.Tenants
            .Include(t => t.AiCreditAccount)
            .FirstOrDefaultAsync(t => t.Id == model.TenantId);

        if (tenant is null)
            return NotFound();

        model.CompanyName = tenant.CompanyName;

        if (!ModelState.IsValid)
            return View("~/Features/AiCredits/Views/AddCredits.cshtml", model);

        // Ensure credit account exists
        if (tenant.AiCreditAccount is null)
        {
            tenant.AiCreditAccount = new AiCreditAccount
            {
                TenantId = tenant.Id,
                ImageGenerationCredits = 0,
                ProductDescriptionCredits = 0
            };
            dbContext.AiCreditAccounts.Add(tenant.AiCreditAccount);
            await dbContext.SaveChangesAsync();
        }

        var creditType = model.CreditType == "ImageGeneration"
            ? AiCreditType.ImageGeneration
            : AiCreditType.ProductDescription;

        // Update balance
        if (creditType == AiCreditType.ImageGeneration)
            tenant.AiCreditAccount.ImageGenerationCredits += model.Amount;
        else
            tenant.AiCreditAccount.ProductDescriptionCredits += model.Amount;

        // Record transaction
        var transaction = new AiCreditTransaction
        {
            AiCreditAccountId = tenant.AiCreditAccount.Id,
            CreditType = creditType,
            Amount = model.Amount,
            Description = model.Description
        };
        dbContext.AiCreditTransactions.Add(transaction);

        tenant.AiCreditAccount.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{model.Amount} kredi basariyla eklendi.";
        return RedirectToAction(nameof(Details), new { tenantId = model.TenantId });
    }
}
