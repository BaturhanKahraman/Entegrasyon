using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.MVC.Infrastructure.Extensions;
using System.Security.Claims;

namespace Entegrasyon.MVC.Features.Notifications;

[Authorize]
public class NotificationController(
    INotificationManager notificationManager,
    INotificationRecipientResolver recipientResolver) : Controller
{
    [HttpGet("/notifications")]
    public async Task<IActionResult> Index(
        string? tab = "all",
        NotificationCategory? category = null,
        NotificationSeverity? severity = null)
    {
        ViewData.SetPageTitle("Bildirimler");
        ViewData.SetActiveNav("notifications");

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notifications = await notificationManager.GetNotificationsPageAsync(
            userId,
            tab ?? "all",
            category,
            severity);

        ViewBag.Tab = tab ?? "all";
        ViewBag.Category = category;
        ViewBag.Severity = severity;
        ViewBag.UserId = userId;
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
    public async Task<IActionResult> Admin()
    {
        ViewData.SetPageTitle("Bildirim Yonetimi");
        ViewData.SetActiveNav("notifications");

        var recentNotifications = await notificationManager.GetAllNotificationsAsync(50);
        ViewBag.RecentNotifications = recentNotifications;

        return View("~/Features/Notifications/Views/Admin.cshtml");
    }

    [HttpPost("/admin/notifications/send")]
    public async Task<IActionResult> Send(
        string title,
        string message,
        NotificationSeverity severity = NotificationSeverity.Info,
        NotificationCategory category = NotificationCategory.Sistem,
        string target = "all")
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Baslik ve mesaj zorunludur.", type = "danger" });
                return StatusCode(422);
            }

            TempData.SetError("Baslik ve mesaj zorunludur.");
            return RedirectToAction(nameof(Admin));
        }

        try
        {
            var userIds = await recipientResolver.ResolveAllActiveUsersAsync();

            if (userIds.Count == 0)
            {
                if (Request.IsHtmx())
                {
                    Response.HtmxTriggerWithData("showToast",
                        new { message = "Bildirim gonderilecek kullanici bulunamadı.", type = "warning" });
                    return StatusCode(422);
                }

                TempData.SetWarning("Bildirim gonderilecek kullanici bulunamadı.");
                return RedirectToAction(nameof(Admin));
            }

            await notificationManager.SendNotification(title, message, severity, category, userIds);

            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = $"Bildirim {userIds.Count} kullaniciya gonderildi.", type = "success" });
                Response.HtmxTrigger("refreshNotifications");
                return Content("");
            }

            TempData.SetSuccess($"Bildirim {userIds.Count} kullaniciya gonderildi.");
        }
        catch (Exception ex)
        {
            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = $"Bildirim gonderilemedi: {ex.Message}", type = "danger" });
                return StatusCode(500);
            }

            TempData.SetError($"Bildirim gonderilemedi: {ex.Message}");
        }

        return RedirectToAction(nameof(Admin));
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
