using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.MVC.Features.IntegrationHealth.ViewModels;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.IntegrationHealth;

[Authorize]
public class IntegrationHealthController(IApplicationLogManager logManager) : HtmxController
{
    [HttpGet("/integrations/health")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Entegrasyon Sagligi");
        ViewData.SetActiveNav("integration-health");
        ViewData.SetBreadcrumb(("Entegrasyon Sagligi", null));

        var syncResult = await logManager.GetPaginatedLogs(
            pageIndex: 0,
            itemCount: 200,
            logType: LogType.Marketplace,
            logAction: LogAction.Sync);

        var errorResult = await logManager.GetPaginatedLogs(
            pageIndex: 0,
            itemCount: 20,
            logType: LogType.Error);

        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);

        var syncLogs = syncResult.Success && syncResult.Data is not null
            ? syncResult.Data.Items
            : [];

        var recentSyncCount = syncLogs
            .Count(l => l.CreatedAt >= cutoff);

        var lastSyncTime = syncLogs.Count > 0
            ? syncLogs.Max(l => l.CreatedAt)
            : (DateTimeOffset?)null;

        var recentErrors = errorResult.Success && errorResult.Data is not null
            ? errorResult.Data.Items.ToList()
            : [];

        var recentErrorCount = recentErrors.Count(l => l.CreatedAt >= cutoff);

        var vm = new IntegrationHealthVm
        {
            RecentSyncCount = recentSyncCount,
            RecentErrorCount = recentErrorCount,
            LastSyncTime = lastSyncTime,
            RecentErrors = recentErrors
        };

        return HtmxView("~/Features/IntegrationHealth/Views/Index.cshtml", vm);
    }
}
