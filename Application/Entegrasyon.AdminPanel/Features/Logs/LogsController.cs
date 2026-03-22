using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.AdminPanel.Infrastructure.Data;

namespace Entegrasyon.AdminPanel.Features.Logs;

[Authorize]
public class LogsController(AdminPanelDbContext dbContext) : Controller
{
    private const int PageSize = 25;

    public async Task<IActionResult> Index(int? tenantId, string? level, DateTime? from, DateTime? to, int page = 1)
    {
        var query = dbContext.ApplicationLogs
            .Include(l => l.Tenant)
            .AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(l => l.TenantId == tenantId.Value);

        if (!string.IsNullOrEmpty(level))
            query = query.Where(l => l.Level == level);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value.ToUniversalTime());

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(l => new LogItemViewModel
            {
                Id = l.Id,
                TenantName = l.Tenant.CompanyName,
                Level = l.Level,
                Message = l.Message,
                Source = l.Source,
                UserName = l.UserName,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        var tenants = await dbContext.Tenants
            .OrderBy(t => t.CompanyName)
            .Select(t => new TenantSelectItem { Id = t.Id, CompanyName = t.CompanyName })
            .ToListAsync();

        var model = new LogListViewModel
        {
            Logs = logs,
            Filter = new LogFilterViewModel
            {
                TenantId = tenantId,
                Level = level,
                From = from,
                To = to
            },
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Tenants = tenants
        };

        return View("~/Features/Logs/Views/Index.cshtml", model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var log = await dbContext.ApplicationLogs
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log is null)
            return NotFound();

        var model = new LogDetailViewModel
        {
            Id = log.Id,
            TenantName = log.Tenant.CompanyName,
            TenantId = log.TenantId,
            Level = log.Level,
            Message = log.Message,
            Source = log.Source,
            StackTrace = log.StackTrace,
            UserName = log.UserName,
            CreatedAt = log.CreatedAt
        };

        return View("~/Features/Logs/Views/Details.cshtml", model);
    }
}
