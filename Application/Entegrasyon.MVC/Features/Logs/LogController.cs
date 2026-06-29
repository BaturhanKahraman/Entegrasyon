using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Logs;

[Authorize]
public class LogController(IApplicationLogManager applicationLogManager) : Controller
{
    [HttpGet("/logs")]
    public async Task<IActionResult> Index(
        LogType? logType = null,
        LogAction? logAction = null,
        string? entityType = null,
        string? entityId = null,
        int page = 1)
    {
        ViewData.SetPageTitle("Sistem Logları");
        ViewData.SetActiveNav("logs");

        ViewBag.LogType = logType;
        ViewBag.LogAction = logAction;
        ViewBag.EntityType = entityType;
        ViewBag.EntityId = entityId;

        // Boş string parametreler null'a normalize edilir (HTML form temiz input için "" gönderir)
        var normalizedEntityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim();
        var normalizedEntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim();

        var result = await applicationLogManager.GetPaginatedLogs(
            page - 1, 50,
            logType,
            logAction,
            normalizedEntityType,
            normalizedEntityId);

        if (Request.IsHtmx())
            return PartialView("~/Features/Logs/Views/Partials/_LogTable.cshtml", result.Data);

        return View("~/Features/Logs/Views/Index.cshtml", result.Data);
    }
}
