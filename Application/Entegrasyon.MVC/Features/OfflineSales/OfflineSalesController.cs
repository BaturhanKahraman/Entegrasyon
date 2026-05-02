using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.OfflineSales;

public sealed record SyncRequest(List<OfflineSaleDto> Sales);
public sealed record SyncResponse(List<OfflineSaleSyncResult> Results);

public class OfflineSalesController(IOfflineSaleSyncManager syncManager) : Controller
{
    [HttpPost("/api/offline-sales/sync")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Sync([FromBody] SyncRequest request)
    {
        if (request is null || request.Sales is null || request.Sales.Count == 0)
            return BadRequest(new { error = "Senkronize edilecek satış yok." });

        // Device middleware'i tarafından eklenen claim'lerden tenantId
        var tenantIdStr = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdStr, out var tenantId))
            return Unauthorized(new { error = "Cihaz auth claim'leri eksik." });

        // Önce-sonra sırasıyla işle (sale içi atomic, sale'ler arası ayrı transaction)
        var ordered = request.Sales.OrderBy(s => s.OccurredAt).ToList();
        var results = new List<OfflineSaleSyncResult>(ordered.Count);
        foreach (var sale in ordered)
        {
            var r = await syncManager.SyncOneAsync(tenantId, sale);
            results.Add(r.Success
                ? r.Data
                : new OfflineSaleSyncResult(sale.IdempotencyKey, "error", null, null, r.Message));
        }

        return Ok(new SyncResponse(results));
    }
}
