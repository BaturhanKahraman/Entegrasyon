using System.Security.Claims;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

[Authorize]
public class AccountController(
    IStorefrontTenantContext tenant,
    IStorefrontAuthManager authManager,
    IOrderManager orderManager,
    IStorefrontReturnManager returnManager) : Controller
{
    private int GetCustomerId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private int GetAuthId() => int.Parse(User.FindFirst("AuthId")!.Value);

    public async Task<IActionResult> Index()
    {
        var authResult = await authManager.GetAuthByCustomerIdAsync(tenant.TenantId, GetCustomerId());
        ViewBag.Auth = authResult.Data;
        ViewBag.CustomerName = User.FindFirst(ClaimTypes.Name)?.Value;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var authResult = await authManager.GetAuthByCustomerIdAsync(tenant.TenantId, GetCustomerId());
        if (!authResult.Success) return RedirectToAction("Index");
        var auth = authResult.Data;
        ViewBag.Profile = new StorefrontProfileDto(auth.Customer.Name, auth.Customer.Surname, auth.Customer.PhoneNumber);
        ViewBag.Email = auth.Email;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(StorefrontProfileDto dto)
    {
        var result = await authManager.UpdateProfileAsync(GetCustomerId(), dto);
        ViewBag.Profile = dto;
        var authResult = await authManager.GetAuthByCustomerIdAsync(tenant.TenantId, GetCustomerId());
        ViewBag.Email = authResult.Data?.Email;
        ViewBag.Success = result.Success;
        ViewBag.Error = result.Success ? null : result.Message;
        return View();
    }

    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Yeni sifreler uyusmuyor.";
            return View();
        }
        var result = await authManager.ChangePasswordAsync(GetAuthId(), currentPassword, newPassword);
        ViewBag.Success = result.Success;
        ViewBag.Error = result.Success ? null : result.Message;
        return View();
    }

    public async Task<IActionResult> Orders()
    {
        var result = await orderManager.GetCustomerOrdersAsync(GetCustomerId(), tenant.TenantId);
        var orders = result.Data?.Select(o => new StorefrontOrderListDto(
            o.Id,
            o.OrderNumber ?? "-",
            o.OrderDate ?? o.CreatedAt,
            o.GrossAmount ?? (o.SubTotal ?? 0) + (o.ShippingCost ?? 0) - (o.DiscountAmount ?? 0),
            o.OrderItems.Count(),
            MapOrderStatus(o.StorefrontOrderStatus),
            o.CargoTrackingNumber
        )).ToList() ?? [];

        ViewBag.Orders = orders;
        return View();
    }

    [HttpGet("/hesabim/siparis/{id:guid}")]
    public async Task<IActionResult> OrderDetail(Guid id)
    {
        var result = await orderManager.GetOrderDetailAsync(id, GetCustomerId());
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Orders");
        }

        var o = result.Data;
        var items = o.OrderItems.Select(i => new StorefrontOrderItemDto(
            i.Product?.Product?.Title ?? i.Barcode ?? "Urun",
            null,
            FormatVariantInfo(i),
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice
        )).ToList();

        var shippingAddr = o.ShippingAddress is not null
            ? $"{o.ShippingAddress.FullAddress}, {o.ShippingAddress.County} {o.ShippingAddress.City}"
            : null;

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
            shippingAddr,
            $"{o.CustomerFirstName} {o.CustomerLastName}".Trim(),
            o.CustomerEmail,
            o.OrderNote,
            items
        );

        ViewBag.Order = detail;
        return View();
    }

    [HttpPost("/hesabim/siparis/{id:guid}/iptal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var result = await orderManager.CancelOrderAsync(id, GetCustomerId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction("OrderDetail", new { id });
    }

    [HttpGet("/hesabim/siparis/{id:guid}/fatura")]
    public async Task<IActionResult> DownloadInvoice(Guid id)
    {
        var result = await orderManager.GetOrderDetailAsync(id, GetCustomerId());
        if (!result.Success)
            return NotFound();

        var o = result.Data;
        var settings = tenant.Settings;
        var html = GenerateInvoiceHtml(o, settings);
        var bytes = Encoding.UTF8.GetBytes(html);
        return File(bytes, "text/html", $"fatura-{o.OrderNumber}.html");
    }

    [HttpGet("/hesabim/iadelerim")]
    public async Task<IActionResult> Returns()
    {
        var result = await returnManager.GetCustomerReturnsAsync(tenant.TenantId, GetCustomerId());
        ViewBag.Returns = result.Data ?? [];
        return View();
    }

    [HttpGet("/hesabim/iade-talebi/{orderId:guid}")]
    public async Task<IActionResult> CreateReturn(Guid orderId)
    {
        var orderResult = await orderManager.GetOrderDetailAsync(orderId, GetCustomerId());
        if (!orderResult.Success)
        {
            TempData["Error"] = orderResult.Message;
            return RedirectToAction("Orders");
        }

        ViewBag.OrderId = orderId;
        ViewBag.OrderNumber = orderResult.Data.OrderNumber;
        return View();
    }

    [HttpPost("/hesabim/iade-talebi/{orderId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReturn(Guid orderId, string reason, string? description)
    {
        var result = await returnManager.CreateReturnRequestAsync(
            tenant.TenantId, orderId, GetCustomerId(), reason, description);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
            return Redirect("/hesabim/iadelerim");
        }

        TempData["Error"] = result.Message;
        var orderResult = await orderManager.GetOrderDetailAsync(orderId, GetCustomerId());
        ViewBag.OrderId = orderId;
        ViewBag.OrderNumber = orderResult.Data?.OrderNumber;
        return View();
    }

    public IActionResult Addresses()
    {
        ViewBag.PageTitle = "Adreslerim";
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

    private static string FormatVariantInfo(Entity.Orders.OrderItem item)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(item.ProductColor)) parts.Add($"Renk: {item.ProductColor}");
        if (!string.IsNullOrEmpty(item.ProductSize)) parts.Add($"Beden: {item.ProductSize}");
        return parts.Count > 0 ? string.Join(" / ", parts) : "";
    }

    private static string GenerateInvoiceHtml(Entity.Orders.Order o, StorefrontSettings settings)
    {
        var sb = new StringBuilder();

        // Item rows
        foreach (var i in o.OrderItems)
        {
            var title = i.Product?.Product?.Title ?? i.Barcode ?? "Urun";
            sb.AppendLine($"<tr>");
            sb.AppendLine($"  <td style=\"padding:8px;border-bottom:1px solid #eee\">{title}</td>");
            sb.AppendLine($"  <td style=\"padding:8px;border-bottom:1px solid #eee;text-align:center\">{i.Quantity}</td>");
            sb.AppendLine($"  <td style=\"padding:8px;border-bottom:1px solid #eee;text-align:right\">{i.UnitPrice:N2} TL</td>");
            sb.AppendLine($"  <td style=\"padding:8px;border-bottom:1px solid #eee;text-align:right\">{i.Quantity * i.UnitPrice:N2} TL</td>");
            sb.AppendLine("</tr>");
        }
        var itemRows = sb.ToString();

        var subTotal = o.SubTotal ?? 0;
        var shipping = o.ShippingCost ?? 0;
        var discount = o.DiscountAmount ?? 0;
        var total = o.GrossAmount ?? subTotal + shipping - discount;
        var orderDate = (o.OrderDate ?? o.CreatedAt).ToString("dd.MM.yyyy");
        var emailLine = o.CustomerEmail is not null ? $"<p>{o.CustomerEmail}</p>" : "";
        var addressLine = o.ShippingAddress is not null
            ? $"<p>{o.ShippingAddress.FullAddress}, {o.ShippingAddress.County} {o.ShippingAddress.City}</p>"
            : "";
        var discountLine = discount > 0 ? $"<p>Indirim: -{discount:N2} TL</p>" : "";

        return
            "<!DOCTYPE html>" +
            "<html lang=\"tr\"><head><meta charset=\"utf-8\"/>" +
            $"<title>Fatura - {o.OrderNumber}</title>" +
            "<style>" +
            "body{font-family:Arial,sans-serif;max-width:800px;margin:0 auto;padding:20px;color:#333}" +
            "h1{font-size:24px;margin-bottom:4px}" +
            ".header{display:flex;justify-content:space-between;margin-bottom:30px}" +
            ".info{margin-bottom:20px}.info p{margin:2px 0}" +
            "table{width:100%;border-collapse:collapse}" +
            "th{background:#f5f5f5;padding:10px 8px;text-align:left;border-bottom:2px solid #ddd}" +
            ".totals{margin-top:20px;text-align:right}.totals p{margin:4px 0}" +
            ".total-line{font-size:18px;font-weight:bold}" +
            ".footer{margin-top:40px;font-size:12px;color:#888;text-align:center;border-top:1px solid #eee;padding-top:10px}" +
            "</style></head><body>" +
            "<div class=\"header\"><div>" +
            $"<h1>{settings.CompanyName}</h1>" +
            $"<p>{settings.Address}, {settings.City}</p>" +
            $"<p>Tel: {settings.ContactPhone} | E-posta: {settings.ContactEmail}</p>" +
            $"<p>Vergi Dairesi: {settings.CompanyTaxOffice} | VKN: {settings.CompanyTaxNumber}</p>" +
            "</div><div style=\"text-align:right\">" +
            "<h2>FATURA</h2>" +
            $"<p>Siparis No: {o.OrderNumber}</p>" +
            $"<p>Tarih: {orderDate}</p>" +
            "</div></div>" +
            "<div class=\"info\">" +
            $"<strong>Musteri:</strong><p>{o.CustomerFirstName} {o.CustomerLastName}</p>" +
            emailLine + addressLine +
            "</div>" +
            "<table><thead><tr>" +
            "<th>Urun</th><th style=\"text-align:center\">Adet</th>" +
            "<th style=\"text-align:right\">Birim Fiyat</th><th style=\"text-align:right\">Toplam</th>" +
            "</tr></thead><tbody>" +
            itemRows +
            "</tbody></table>" +
            "<div class=\"totals\">" +
            $"<p>Ara Toplam: {subTotal:N2} TL</p>" +
            $"<p>Kargo: {shipping:N2} TL</p>" +
            discountLine +
            $"<p class=\"total-line\">Genel Toplam: {total:N2} TL</p>" +
            "</div>" +
            "<div class=\"footer\">" +
            $"<p>{settings.CompanyName} | {settings.CompanyTaxOffice} VD | {settings.CompanyTaxNumber}</p>" +
            "<p>Bu belge bilgilendirme amaclidir. Resmi fatura yerine gecmez.</p>" +
            "</div></body></html>";
    }
}
