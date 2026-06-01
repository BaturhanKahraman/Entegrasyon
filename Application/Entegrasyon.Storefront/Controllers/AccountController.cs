using System.Security.Claims;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Entegrasyon.Storefront.Controllers;

[Authorize]
public class AccountController(
    IStorefrontTenantContext tenant,
    IStorefrontAuthManager authManager,
    IOrderManager orderManager,
    IStorefrontReturnManager returnManager,
    IStorefrontLoyaltyManager loyaltyManager,
    IStorefrontReferralManager referralManager,
    IStorefrontWalletManager walletManager,
    IStorefrontAddressManager addressManager,
    IMinioFileStorage fileStorage) : Controller
{
    private int GetCustomerId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private int GetAuthId() => int.Parse(User.FindFirst("AuthId")!.Value);

    // Tüm hesap sayfalarında _AccountSidebar partial'ı CustomerName + CustomerTier okur.
    // Her action'da tek tek set etmek yerine burada merkezi olarak claim'lerden doldurulur;
    // böylece sidebar hiçbir sayfada "Ayşe Yılmaz / Bronze Üye" demo verisine düşmez.
    // NOT: Üyelik kademesi (tier) için henüz backend alanı yok — backend-gaps "MembershipTier"
    // claim'ini login'de set ederse buradan otomatik okunur, yoksa nötr "Üye" gösterilir.
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewBag.CustomerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Üye";
        ViewBag.CustomerTier = User.FindFirst("MembershipTier")?.Value ?? "Üye";
        base.OnActionExecuting(context);
    }

    public async Task<IActionResult> Index()
    {
        var authResult = await authManager.GetAuthByCustomerIdAsync(tenant.TenantId, GetCustomerId());
        ViewBag.Auth = authResult.Data;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var authResult = await authManager.GetAuthByCustomerIdAsync(tenant.TenantId, GetCustomerId());
        if (!authResult.Success) return RedirectToAction("Index");
        var auth = authResult.Data;
        var customer = auth.Customer;

        ViewBag.Name = customer?.Name;
        ViewBag.Surname = customer?.Surname;
        ViewBag.Email = auth.Email;
        ViewBag.Phone = customer?.PhoneNumber;
        ViewBag.BirthDate = customer?.BirthDate?.ToString("yyyy-MM-dd");
        ViewBag.Gender = customer?.Gender;
        ViewBag.NewsletterOptIn = auth.MarketingConsent;
        ViewBag.AvatarUrl = customer?.AvatarUrl;
        ViewBag.EmailConfirmed = auth.EmailConfirmed;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(StorefrontProfileDto dto, IFormFile? avatar)
    {
        var customerId = GetCustomerId();

        // Avatar yüklenmişse MinIO'ya kaydet, public URL'i DTO'ya ekle.
        if (avatar is { Length: > 0 })
        {
            var ext = Path.GetExtension(avatar.FileName);
            var objectName = $"avatars/{tenant.TenantId}/{customerId}-{Guid.NewGuid():N}{ext}";
            await using var stream = avatar.OpenReadStream();
            await fileStorage.UploadAsync(stream, objectName, avatar.ContentType);
            dto = dto with { AvatarUrl = fileStorage.GetPublicUrl(objectName) };
        }

        var result = await authManager.UpdateProfileAsync(tenant.TenantId, customerId, dto);
        TempData[result.Success ? "ProfileSuccess" : "ProfileError"] = result.Message;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet("/hesabim/e-posta-degistir")]
    public async Task<IActionResult> ChangeEmail()
    {
        var authResult = await authManager.GetAuthByCustomerIdAsync(tenant.TenantId, GetCustomerId());
        ViewBag.Email = authResult.Data?.Email;
        return View();
    }

    [HttpPost("/hesabim/e-posta-degistir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(string newEmail, string currentPassword)
    {
        var result = await authManager.RequestEmailChangeAsync(
            tenant.TenantId, GetAuthId(), newEmail, currentPassword);

        if (result.Success)
        {
            TempData["ProfileSuccess"] = result.Message;
            return RedirectToAction(nameof(Profile));
        }

        TempData["ProfileError"] = result.Message;
        var authResult = await authManager.GetAuthByCustomerIdAsync(tenant.TenantId, GetCustomerId());
        ViewBag.Email = authResult.Data?.Email;
        return View();
    }

    [HttpGet("/hesabim/sifre")]
    public IActionResult ChangePassword() => View();

    [HttpPost("/hesabim/sifre")]
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

    [HttpGet("/hesabim/Sipariş/{id:guid}")]
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

    [HttpPost("/hesabim/Sipariş/{id:guid}/iptal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var result = await orderManager.CancelOrderAsync(id, GetCustomerId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction("OrderDetail", new { id });
    }

    [HttpGet("/hesabim/Sipariş/{id:guid}/fatura")]
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

    public async Task<IActionResult> Addresses()
    {
        var result = await addressManager.GetCustomerAddressesAsync(tenant.TenantId, GetCustomerId());
        ViewBag.Addresses = result.Data ?? new List<Entity.Storefront.StorefrontAddress>();
        return View();
    }

    [HttpPost("/hesabim/adres-kaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAddress(StorefrontAddressDto dto)
    {
        var customerId = GetCustomerId();
        var result = dto.Id > 0
            ? await addressManager.UpdateAsync(tenant.TenantId, customerId, dto.Id, dto)
            : await addressManager.AddAsync(tenant.TenantId, customerId, dto);

        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("/hesabim/adres-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var result = await addressManager.DeleteAsync(tenant.TenantId, GetCustomerId(), id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("/hesabim/adres-varsayilan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var result = await addressManager.SetDefaultAsync(tenant.TenantId, GetCustomerId(), id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Addresses));
    }

    [HttpGet("/hesabim/tekrar-satin-al")]
    public async Task<IActionResult> BuyAgain()
    {
        var result = await orderManager.GetPreviouslyPurchasedProductsAsync(GetCustomerId());
        ViewBag.Products = result.Data ?? [];
        return View();
    }

    [HttpPost("/hesabim/tekrar-satin-al/{orderId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderFromOrder(Guid orderId)
    {
        var orderResult = await orderManager.GetOrderDetailAsync(orderId, GetCustomerId());
        if (!orderResult.Success)
        {
            TempData["Error"] = orderResult.Message;
            return RedirectToAction("Orders");
        }

        // We need cart access — redirect with items info
        TempData["Success"] = "Siparişteki urunler sepete eklendi.";
        return Redirect("/sepet");
    }

    [HttpGet("/hesabim/guvenlik")]
    public async Task<IActionResult> Security()
    {
        var authResult = await authManager.GetAuthByCustomerIdAsync(tenant.TenantId, GetCustomerId());
        var auth = authResult.Data;

        var historyResult = await authManager.GetLoginHistoryAsync(GetAuthId());
        ViewBag.LoginHistory = historyResult.Data ?? [];

        ViewBag.TwoFactorEnabled = auth?.TwoFactorEnabled ?? false;
        ViewBag.TwoFactorMethod = "Authenticator uygulaması";
        ViewBag.LoginAlerts = auth?.LoginAlertsEnabled ?? true;
        ViewBag.LastPasswordChange = auth?.LastPasswordChangedAt; // DateTimeOffset? — view formatlar
        return View();
    }

    [HttpPost("/hesabim/giris-bildirimleri")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLoginAlerts(bool enabled)
    {
        var result = await authManager.SetLoginAlertsAsync(GetAuthId(), enabled);
        TempData[result.Success ? "ProfileSuccess" : "ProfileError"] = result.Message;
        return RedirectToAction(nameof(Security));
    }

    [HttpPost("/hesabim/hesabimi-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string password, string confirmation)
    {
        if (!string.Equals(confirmation?.Trim(), "SIL", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ProfileError"] = "Hesabı silmek için onay kutusuna 'SIL' yazmalısınız.";
            return RedirectToAction(nameof(Security));
        }

        var result = await authManager.DeleteAccountAsync(tenant.TenantId, GetAuthId(), password);
        if (!result.Success)
        {
            TempData["ProfileError"] = result.Message;
            return RedirectToAction(nameof(Security));
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Hesabınız silindi. İlginiz için teşekkür ederiz.";
        return Redirect("/");
    }

    [HttpGet("/hesabim/veri-indir")]
    public async Task<IActionResult> ExportData()
    {
        var result = await authManager.ExportCustomerDataAsync(tenant.TenantId, GetCustomerId());
        if (!result.Success)
            return NotFound();

        var bytes = System.Text.Encoding.UTF8.GetBytes(result.Data);
        return File(bytes, "application/json", $"kvkk-veri-export-{DateTime.Now:yyyyMMdd}.json");
    }

    [HttpGet("/hesabim/puan-programi")]
    public async Task<IActionResult> LoyaltyPoints()
    {
        var customerId = GetCustomerId();
        var balanceResult = await loyaltyManager.GetBalanceAsync(tenant.TenantId, customerId);
        var transactionsResult = await loyaltyManager.GetTransactionsAsync(tenant.TenantId, customerId);

        ViewBag.Balance = balanceResult.Data;
        ViewBag.Transactions = transactionsResult.Data ?? new List<Entity.Storefront.StorefrontLoyaltyTransaction>();
        ViewBag.Settings = tenant.Settings;
        return View();
    }

    [HttpGet("/hesabim/arkadasini-getir")]
    public async Task<IActionResult> Referral()
    {
        var customerId = GetCustomerId();
        var codeResult = await referralManager.GetOrCreateReferralCodeAsync(tenant.TenantId, customerId);
        var referralsResult = await referralManager.GetReferralsAsync(tenant.TenantId, customerId);

        ViewBag.ReferralCode = codeResult.Data;
        ViewBag.Referrals = referralsResult.Data ?? new List<Entity.Storefront.StorefrontReferral>();
        ViewBag.Domain = tenant.Domain.DomainName;
        ViewBag.Settings = tenant.Settings;
        return View();
    }

    [HttpGet("/hesabim/cuzdanim")]
    public async Task<IActionResult> Wallet()
    {
        var customerId = GetCustomerId();
        var walletResult = await walletManager.GetOrCreateWalletAsync(tenant.TenantId, customerId);
        var transactionsResult = await walletManager.GetTransactionsAsync(tenant.TenantId, customerId);

        var transactions = transactionsResult.Data ?? new List<Entity.Storefront.StorefrontWalletTransaction>();

        // Mini istatistikler: HTML 3 kart (Toplam Yüklenen / Harcanan / Cashback).
        // Doğrudan enum eşlemesi: TopUp→yükleme, Cashback→cashback, OrderPayment→harcama.
        // Promotion (legacy/backward-compat) genel "yükleme" sayılır; Refund istatistiğe girmez
        // (iade gerçek bir yükleme/harcama değil, ayrı kategoride gösterilir).
        decimal totalLoaded = 0m, totalSpent = 0m, totalCashback = 0m;
        foreach (var t in transactions)
        {
            switch (t.TransactionType)
            {
                case Entity.Storefront.WalletTransactionType.TopUp:
                case Entity.Storefront.WalletTransactionType.Promotion:
                    totalLoaded += t.Amount;
                    break;
                case Entity.Storefront.WalletTransactionType.Cashback:
                    totalCashback += t.Amount;
                    break;
                case Entity.Storefront.WalletTransactionType.OrderPayment:
                    totalSpent += Math.Abs(t.Amount);
                    break;
                // Refund: istatistiklerde sayılmaz.
            }
        }

        ViewBag.Wallet = walletResult.Data;
        ViewBag.Transactions = transactions;
        ViewBag.TotalLoaded = totalLoaded;
        ViewBag.TotalSpent = totalSpent;
        ViewBag.TotalCashback = totalCashback;
        return View();
    }

    [HttpGet("/hesabim/guvenlik/2fa")]
    public async Task<IActionResult> TwoFactorSetup()
    {
        var authId = GetAuthId();
        var isEnabledResult = await authManager.IsTwoFactorEnabledAsync(authId);
        ViewBag.TwoFactorEnabled = isEnabledResult.Data;
        return View();
    }

    [HttpPost("/hesabim/guvenlik/2fa/etkinlestir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactor()
    {
        var authId = GetAuthId();
        var result = await authManager.Enable2FAAsync(authId);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return Redirect("/hesabim/guvenlik/2fa");
        }

        ViewBag.QrCodeUri = result.Data;
        ViewBag.TwoFactorEnabled = false;
        ViewBag.ShowVerification = true;
        return View("TwoFactorSetup");
    }

    [HttpPost("/hesabim/guvenlik/2fa/dogrula")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyTwoFactor(string code)
    {
        var authId = GetAuthId();
        var result = await authManager.Verify2FAAsync(authId, code);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return Redirect("/hesabim/guvenlik/2fa");
        }

        // Generate recovery codes
        var codesResult = await authManager.GenerateRecoveryCodesAsync(authId);
        ViewBag.RecoveryCodes = codesResult.Data;
        ViewBag.TwoFactorEnabled = true;
        TempData["Success"] = "Iki faktorlu dogrulama etkinlestirildi.";
        return View("TwoFactorSetup");
    }

    [HttpPost("/hesabim/guvenlik/2fa/devre-disi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactor(string code)
    {
        var authId = GetAuthId();
        var result = await authManager.Disable2FAAsync(authId, code);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return Redirect("/hesabim/guvenlik/2fa");
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
            $"<p>Sipariş No: {o.OrderNumber}</p>" +
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
