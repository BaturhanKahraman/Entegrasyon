using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class GiftCardController(
    IStorefrontTenantContext tenant,
    IStorefrontGiftCardManager giftCardManager) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.SeoTitle = $"Hediye Karti | {tenant.Settings.StoreName}";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(decimal amount, string? recipientEmail, string? recipientName, string? message)
    {
        int? customerId = User.Identity?.IsAuthenticated == true
            ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
            : null;

        var result = await giftCardManager.CreateGiftCardAsync(
            tenant.TenantId, amount, customerId, recipientEmail, recipientName, message);

        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            ViewBag.SeoTitle = $"Hediye Karti | {tenant.Settings.StoreName}";
            return View();
        }

        ViewBag.GiftCard = result.Data;
        ViewBag.SeoTitle = $"Hediye Karti Olusturuldu | {tenant.Settings.StoreName}";
        return View("Created");
    }

    [HttpGet]
    public IActionResult Balance()
    {
        ViewBag.SeoTitle = $"Hediye Karti Sorgula | {tenant.Settings.StoreName}";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Balance(string code)
    {
        ViewBag.SeoTitle = $"Hediye Karti Sorgula | {tenant.Settings.StoreName}";

        var result = await giftCardManager.CheckBalanceAsync(tenant.TenantId, code);

        ViewBag.Code = code;
        ViewBag.BalanceResult = result;
        return View();
    }
}
