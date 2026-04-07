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
    IPaymentGatewayService paymentGateway,
    IStorefrontEmailService emailService,
    ISellerCommissionManager sellerCommissionManager) : Controller
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

        var order = result.Data;

        // If order is Pending (iyzico configured), initiate payment
        if (order.StorefrontPaymentStatus == Entity.Storefront.PaymentStatus.Pending)
        {
            var nameParts = dto.ShippingFullName.Split(' ', 2);
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/odeme/callback";

            var paymentItems = order.OrderItems.Select(oi => new PaymentItemDto(
                Name: oi.Barcode ?? oi.ProductId.ToString()!,
                Category: "Genel",
                Price: oi.UnitPrice * oi.Quantity,
                Id: oi.ProductId.ToString()!
            )).ToList();

            var paymentRequest = new PaymentRequest(
                OrderId: order.Id,
                Amount: order.GrossAmount ?? 0,
                CustomerEmail: User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                CustomerName: nameParts[0],
                CustomerSurname: nameParts.Length > 1 ? nameParts[1] : "",
                CustomerPhone: dto.ShippingPhone,
                CustomerIp: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                CustomerCity: dto.ShippingCity,
                CustomerAddress: dto.ShippingAddress,
                CallbackUrl: callbackUrl,
                Items: paymentItems
            );

            var paymentResult = await paymentGateway.InitiatePaymentAsync(paymentRequest);

            if (paymentResult.Success)
            {
                // Store orderId in session for callback
                HttpContext.Session.SetString("PendingOrderId", order.Id.ToString());
                return Redirect(paymentResult.Data.PaymentPageUrl);
            }

            // Payment initiation failed — fail the order and show error
            await checkoutManager.FailOrderPaymentAsync(order.Id, paymentResult.Message);
            ViewBag.Error = paymentResult.Message;
            return View("Basarisiz");
        }

        // No iyzico configured — direct order (development/testing fallback)
        if (tenant.Settings.MarketplaceEnabled)
            _ = sellerCommissionManager.ProcessOrderCommissionsAsync(order.Id);

        SendConfirmationEmail(order);
        HttpContext.Session.Remove("CartId");
        return RedirectToAction("Basarili", new { id = order.Id });
    }

    [AllowAnonymous]
    public async Task<IActionResult> Callback(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Basarisiz");

        var callbackResult = await paymentGateway.HandleCallbackAsync(token);

        if (!callbackResult.Success)
            return RedirectToAction("Basarisiz");

        var payment = callbackResult.Data;
        var orderIdStr = HttpContext.Session.GetString("PendingOrderId");

        if (orderIdStr == null || !Guid.TryParse(orderIdStr, out var orderId))
            return RedirectToAction("Basarisiz");

        if (payment.Success)
        {
            await checkoutManager.CompleteOrderPaymentAsync(
                orderId, payment.TransactionId!, payment.PaidAmount ?? 0);

            // Process seller commissions if marketplace is enabled
            if (tenant.Settings.MarketplaceEnabled)
                _ = sellerCommissionManager.ProcessOrderCommissionsAsync(orderId);

            HttpContext.Session.Remove("CartId");
            HttpContext.Session.Remove("PendingOrderId");

            return RedirectToAction("Basarili", new { id = orderId });
        }

        await checkoutManager.FailOrderPaymentAsync(orderId, payment.ErrorMessage);
        HttpContext.Session.Remove("PendingOrderId");
        return RedirectToAction("Basarisiz");
    }

    public async Task<IActionResult> Basarili(Guid id)
    {
        ViewBag.OrderId = id;
        ViewBag.SeoTitle = $"Sipariş Onaylandi | {tenant.Settings.StoreName}";
        ViewBag.EstimatedDeliveryDays = tenant.Settings.EstimatedDeliveryDays;
        return View();
    }

    public IActionResult Basarisiz()
    {
        ViewBag.SeoTitle = $"Odeme Basarisiz | {tenant.Settings.StoreName}";
        return View();
    }

    private void SendConfirmationEmail(Entity.Orders.Order order)
    {
        var customerEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        var customerName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (customerEmail != null)
        {
            _ = emailService.SendOrderConfirmationAsync(
                customerEmail, customerName ?? "Musterimiz",
                order.OrderNumber!,
                order.GrossAmount ?? 0,
                tenant.Settings.StoreName, tenant.Domain.DomainName);
        }
    }
}
