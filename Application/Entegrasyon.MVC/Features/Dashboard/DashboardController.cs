using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Dashboard;

[Authorize]
public class DashboardController(IDashboardManager dashboardManager, IReportManager reportManager) : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        ViewData.SetPageTitle("Dashboard");
        ViewData.SetActiveNav("dashboard");
        return View();
    }

    /// <summary>HTMX lazy-load: stat kartları</summary>
    [HttpGet("/dashboard/stats")]
    public async Task<IActionResult> Stats()
    {
        var stats = await dashboardManager.GetStatsAsync();
        return PartialView("Partials/_StatsCards", stats);
    }

    /// <summary>HTMX lazy-load: haftalık satış grafiği</summary>
    [HttpGet("/dashboard/weekly-sales")]
    public async Task<IActionResult> WeeklySales()
    {
        var sales = await dashboardManager.GetWeeklySalesAsync();
        return PartialView("Partials/_WeeklyChart", sales);
    }

    /// <summary>HTMX lazy-load: marketplace durumları</summary>
    [HttpGet("/dashboard/marketplace-status")]
    public async Task<IActionResult> MarketplaceStatus()
    {
        var statuses = await dashboardManager.GetMarketplaceStatusesAsync();
        return PartialView("Partials/_MarketplaceStatus", statuses);
    }

    /// <summary>HTMX lazy-load: son aktiviteler</summary>
    [HttpGet("/dashboard/recent-activity")]
    public async Task<IActionResult> RecentActivity()
    {
        var activities = await dashboardManager.GetRecentActivitiesAsync();
        return PartialView("Partials/_RecentActivity", activities);
    }

    /// <summary>HTMX lazy-load: 30 gunluk ciro trendi</summary>
    [HttpGet("/dashboard/profit-chart")]
    public async Task<IActionResult> ProfitChart()
    {
        var filter = new SalesReportFilterDto(
            StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
            EndDate: DateOnly.FromDateTime(DateTime.Today));

        var report = await reportManager.GetSalesReportAsync(filter);
        return PartialView("Partials/_ProfitChart", report);
    }

    /// <summary>Hizli islemler paneli (statik partial)</summary>
    [HttpGet("/dashboard/quick-actions")]
    public IActionResult QuickActions()
    {
        return PartialView("Partials/_QuickActions");
    }
}
