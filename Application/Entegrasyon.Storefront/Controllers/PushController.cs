using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class PushController(
    IStorefrontTenantContext tenant,
    IStorefrontPushManager pushManager) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
            return BadRequest(new { success = false, message = "Endpoint zorunludur." });

        int? customerId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var cid))
                customerId = cid;
        }

        var result = await pushManager.SubscribeAsync(
            tenant.TenantId, customerId, request.Endpoint, request.P256dh ?? "", request.Auth ?? "");

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
            return BadRequest(new { success = false, message = "Endpoint zorunludur." });

        var result = await pushManager.UnsubscribeAsync(request.Endpoint);
        return Json(new { success = result.Success, message = result.Message });
    }
}

public record PushSubscriptionRequest(string Endpoint, string? P256dh, string? Auth);
public record PushUnsubscribeRequest(string Endpoint);
