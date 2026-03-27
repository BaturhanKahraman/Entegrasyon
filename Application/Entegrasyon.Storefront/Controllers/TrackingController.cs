using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class TrackingController(
    IOrderManager orderManager) : Controller
{
    [HttpGet]
    public IActionResult Index(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return View();

        return RedirectToAction("Result", new { orderNumber });
    }

    [HttpGet("/siparis-takip/sonuc")]
    public async Task<IActionResult> Result(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return RedirectToAction("Index");

        var result = await orderManager.GetOrderByNumberAsync(orderNumber.Trim());
        if (!result.Success)
        {
            ViewBag.Error = "Siparis bulunamadi. Lutfen siparis numaranizi kontrol edin.";
            ViewBag.SearchedNumber = orderNumber;
            return View("Index");
        }

        var o = result.Data;
        var detail = new StorefrontOrderDetailDto(
            o.Id,
            o.OrderNumber ?? "-",
            o.OrderDate ?? o.CreatedAt,
            o.SubTotal ?? 0,
            o.ShippingCost ?? 0,
            o.DiscountAmount ?? 0,
            o.GrossAmount ?? (o.SubTotal ?? 0) + (o.ShippingCost ?? 0) - (o.DiscountAmount ?? 0),
            MapOrderStatus(o.StorefrontOrderStatus),
            MapPaymentStatus(o.StorefrontPaymentStatus),
            o.CargoTrackingNumber,
            o.CargoTrackingLink,
            o.CargoProviderName,
            null,
            $"{o.CustomerFirstName} {o.CustomerLastName}".Trim(),
            null, // Don't expose email on public tracking page
            null,
            o.OrderItems.Select(i => new StorefrontOrderItemDto(
                i.Product?.Product?.Title ?? i.Barcode ?? "Urun",
                null,
                "",
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice
            )).ToList()
        );

        ViewBag.Order = detail;
        return View();
    }

    private static string MapOrderStatus(OrderStatus? status) => status switch
    {
        OrderStatus.Received => "Alindi",
        OrderStatus.Preparing => "Hazirlaniyor",
        OrderStatus.Shipped => "Kargoda",
        OrderStatus.Delivered => "Teslim Edildi",
        OrderStatus.Cancelled => "Iptal Edildi",
        OrderStatus.Returned => "Iade Edildi",
        _ => "Bilinmiyor"
    };

    private static string MapPaymentStatus(PaymentStatus? status) => status switch
    {
        PaymentStatus.Pending => "Bekliyor",
        PaymentStatus.Paid => "Odendi",
        PaymentStatus.Failed => "Basarisiz",
        PaymentStatus.Refunded => "Iade Edildi",
        _ => "Bilinmiyor"
    };
}
