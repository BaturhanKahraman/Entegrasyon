using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Infrastructure.Extensions;
using System.Security.Claims;

namespace Entegrasyon.MVC.Features.Notifications;

[Authorize]
public class NotificationController(INotificationManager notificationManager) : Controller
{
    [HttpGet("/notifications")]
    public async Task<IActionResult> Index(bool onlyUnread = false)
    {
        ViewData.SetPageTitle("Bildirimler");
        ViewData.SetActiveNav("notifications");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notifications = await notificationManager.GetNotificationsForUser(userId, onlyUnread);

        ViewBag.OnlyUnread = onlyUnread;
        return View(notifications);
    }

    [HttpPost("/notifications/{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await notificationManager.MarkAsRead(id, userId);

        if (Request.IsHtmx())
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Bildirim okundu olarak isaretlendi.", type = "success" });
            return Content("");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/notifications/read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await notificationManager.MarkAllAsRead(userId);

        if (Request.IsHtmx())
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Tum bildirimler okundu.", type = "success" });
            return Content("");
        }

        TempData.SetSuccess("Tum bildirimler okundu olarak isaretlendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/admin/notifications")]
    public IActionResult Admin()
    {
        ViewData.SetPageTitle("Bildirim Yonetimi");
        ViewData.SetActiveNav("notifications");
        return View("~/Features/Notifications/Views/Admin.cshtml");
    }

    [HttpPost("/notifications/{id:long}/dismiss")]
    public async Task<IActionResult> Dismiss(long id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await notificationManager.DismissNotification(id, userId);

        if (Request.IsHtmx())
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Bildirim kaldirildi.", type = "success" });
            return Content("");
        }

        return RedirectToAction(nameof(Index));
    }
}
