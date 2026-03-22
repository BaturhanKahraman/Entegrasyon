using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.Dashboard;

[Authorize]
public class DashboardController(AdminPanelDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var thirtyDaysLater = now.AddDays(30);

        var totalTenants = await dbContext.Tenants.CountAsync();
        var activeTenants = await dbContext.Tenants.CountAsync(t => t.IsActive);

        var expiringLicenses = await dbContext.TenantLicenses
            .CountAsync(l => l.EndDate >= now && l.EndDate <= thirtyDaysLater);

        var totalLogs = await dbContext.ApplicationLogs
            .CountAsync(l => l.CreatedAt >= todayStart);

        var recentTenants = await dbContext.Tenants
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new RecentTenantViewModel
            {
                Id = t.Id,
                CompanyName = t.CompanyName,
                Subdomain = t.Subdomain,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        var recentLogs = await dbContext.ApplicationLogs
            .Include(l => l.Tenant)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new RecentLogViewModel
            {
                Id = l.Id,
                TenantName = l.Tenant.CompanyName,
                Level = l.Level,
                Message = l.Message,
                Source = l.Source,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        var model = new DashboardViewModel
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            ExpiringLicenses = expiringLicenses,
            TotalLogs = totalLogs,
            RecentTenants = recentTenants,
            RecentLogs = recentLogs
        };

        return View("~/Features/Dashboard/Views/Index.cshtml", model);
    }
}
