using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

[Authorize]
public class CheckoutController(
    IStorefrontTenantContext tenant,
    ICartManager cartManager,
    ICheckoutManager checkoutManager,
    IStorefrontEmailService emailService) : Controller
{
    private int GetCustomerId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public async Task<IActionResult> Index()
    {
        var cartIdStr = HttpContext.Session.GetString("CartId");
        if (cartIdStr == null || !Guid.TryParse(cartIdStr, out var cartId))
            return Redirect("/sepet");

        var s = tenant.Settings;
        var cartResult = await cartManager.GetCartDtoAsync(cartId, s.FreeShippingThreshold, s.FlatShippingRate);
        if (!cartResult.Success || cartResult.Data.ItemCount == 0)
            return Redirect("/sepet");

        ViewBag.Cart = cartResult.Data;
        ViewBag.SeoTitle = $"Odeme | {s.StoreName}";
        ViewBag.CustomerName = User.FindFirst(ClaimTypes.Name)?.Value;
        ViewBag.CustomerEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(CheckoutRequestDto dto)
    {
        var cartIdStr = HttpContext.Session.GetString("CartId");
        if (cartIdStr == null || !Guid.TryParse(cartIdStr, out var cartId))
            return Redirect("/sepet");

        var s = tenant.Settings;
        var customerId = GetCustomerId();

        var result = await checkoutManager.CreateOrderFromCartAsync(
            cartId, customerId, tenant.TenantId, dto, s.FreeShippingThreshold, s.FlatShippingRate);

        if (!result.Success)
        {
            var cartResult = await cartManager.GetCartDtoAsync(cartId, s.FreeShippingThreshold, s.FlatShippingRate);
            ViewBag.Cart = cartResult.Data;
            ViewBag.Error = result.Message;
            return View("Index");
        }

        // Send order confirmation email (fire-and-forget)
        var customerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var customerName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        if (customerEmail != null)
        {
            _ = emailService.SendOrderConfirmationAsync(
                customerEmail, customerName ?? "Musterimiz",
                result.Data.OrderNumber!,
                result.Data.GrossAmount ?? 0,
                s.StoreName, tenant.Domain.DomainName);
        }

        // Clear cart session
        HttpContext.Session.Remove("CartId");

        return RedirectToAction("Basarili", new { id = result.Data.Id });
    }

    public async Task<IActionResult> Basarili(Guid id)
    {
        ViewBag.OrderId = id;
        ViewBag.SeoTitle = $"Siparis Onaylandi | {tenant.Settings.StoreName}";
        ViewBag.EstimatedDeliveryDays = tenant.Settings.EstimatedDeliveryDays;
        return View();
    }

    public IActionResult Basarisiz()
    {
        ViewBag.SeoTitle = $"Odeme Basarisiz | {tenant.Settings.StoreName}";
        return View();
    }
}
