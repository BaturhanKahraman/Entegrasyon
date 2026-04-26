using System.Security.Claims;
using Entegrasyon.Business.Notifications.WebPush;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Entegrasyon.MVC.Features.Notifications;

[Authorize]
public sealed class AdminPushController(
    IAdminPushSubscriptionManager manager,
    IOptions<WebPushOptions> options) : Controller
{
    [HttpGet("/admin-push/vapid-public-key")]
    public IActionResult VapidPublicKey() => Content(options.Value.VapidPublicKey, "text/plain");

    [HttpPost("/admin-push/subscribe")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto)
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return Unauthorized();
        }
        var result = await manager.SubscribeAsync(
            userId, dto.Endpoint, dto.P256dh, dto.Auth,
            Request.Headers.UserAgent.ToString());
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    [HttpPost("/admin-push/unsubscribe")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto)
    {
        var result = await manager.UnsubscribeAsync(dto.Endpoint);
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    public sealed record SubscribeDto(string Endpoint, string P256dh, string Auth);
    public sealed record UnsubscribeDto(string Endpoint);
}
