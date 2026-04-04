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
    public async Task<IActionResult> Index(LogType? logType = null, LogAction? logAction = null, int page = 1)
    {
        ViewData.SetPageTitle("Sistem Loglari");
        ViewData.SetActiveNav("logs");

        ViewBag.LogType = logType;
        ViewBag.LogAction = logAction;

        var result = await applicationLogManager.GetPaginatedLogs(page - 1, 50, logType, logAction);

        if (Request.IsHtmx())
            return PartialView("~/Features/Logs/Views/Partials/_LogTable.cshtml", result.Data);

        return View("~/Features/Logs/Views/Index.cshtml", result.Data);
    }
}
