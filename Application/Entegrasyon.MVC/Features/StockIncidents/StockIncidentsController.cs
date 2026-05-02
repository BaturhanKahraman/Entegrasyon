using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Stock;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Entegrasyon.MVC.Features.StockIncidents;

public sealed class IncidentsListVm
{
    public List<NegativeStockIncident> Active { get; set; } = [];
    public int ActiveCount { get; set; }
}

[Authorize]
public class StockIncidentsController(
    INegativeStockIncidentManager incidentManager,
    ITenantContext tenantContext) : Controller
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    [HttpGet("/incidents")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Stok Olayları");
        ViewData.SetActiveNav("incidents");

        var vm = new IncidentsListVm
        {
            Active = await incidentManager.GetActiveAsync(TenantId)
        };
        vm.ActiveCount = vm.Active.Count;
        return View(vm);
    }

    [HttpPost("/incidents/{id:int}/resolve/stock-added")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveStockAdded(int id, [FromForm] string? notes)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            TempData.SetError("Kullanıcı kimliği alınamadı.");
            return RedirectToAction(nameof(Index));
        }

        var dto = new ResolveIncidentDto(NegativeStockResolution.StockAdded, userId, notes);
        var result = await incidentManager.ResolveAsync(id, TenantId, dto);

        if (result.Success)
            TempData.SetSuccess("Olay 'stok ekle' ile çözümlendi. Stok düzeltmesini ayrıca yapmayı unutmayın.");
        else
            TempData.SetError(result.Message ?? "Olay çözümlenemedi.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/incidents/{id:int}/resolve/online-cancelled")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveOnlineCancelled(int id, [FromForm] string? notes)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            TempData.SetError("Kullanıcı kimliği alınamadı.");
            return RedirectToAction(nameof(Index));
        }

        var dto = new ResolveIncidentDto(NegativeStockResolution.OnlineSaleCancelled, userId, notes);
        var result = await incidentManager.ResolveAsync(id, TenantId, dto);

        if (result.Success)
            TempData.SetSuccess("Olay 'online iptal' ile çözümlendi. İlgili siparişi marketplace tarafından da iptal etmeyi unutmayın.");
        else
            TempData.SetError(result.Message ?? "Olay çözümlenemedi.");

        return RedirectToAction(nameof(Index));
    }
}
