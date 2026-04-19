using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.POS;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.Features.POS.ViewModels;
using Entegrasyon.MVC.Features.Sales.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.POS;

[Authorize]
public class POSController(
    IPOSSessionManager posSessionManager,
    ISaleManager saleManager,
    IProductService productService,
    ICustomerManager customerManager,
    IPaymentMethodManager paymentMethodManager,
    IOfficeStockManager officeStockManager,
    IDiscountReasonManager discountReasonManager,
    ITenantContext tenantContext) : Controller
{
    // ── Constants ────────────────────────────────────────────────────────

    private static class DiscountTypes
    {
        public const string Percent = "percent";
        public const string Amount = "amount";
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetCurrentUserName()
        => User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name) ?? "Kasiyer";

    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    // Varsayilan branch office — ilerde claims'ten alinabilir
    private const int DefaultBranchOfficeId = 1;

    private int? GetCustomerIdFromSession()
        => HttpContext.Session.GetInt32("pos_customer_id");

    // ── Main Page ────────────────────────────────────────────────────────

    [HttpGet("/pos")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("POS Terminali");
        ViewData.SetActiveNav("pos");

        var sessionResult = await posSessionManager.GetActiveSessionAsync(DefaultBranchOfficeId);
        var vm = new POSTerminalVm
        {
            CashierName = GetCurrentUserName(),
            HasActiveSession = sessionResult.Success && sessionResult.Data is not null,
            CustomerId = HttpContext.Session.GetInt32("pos_customer_id"),
            CustomerName = HttpContext.Session.GetString("pos_customer_name")
        };

        if (vm.HasActiveSession)
        {
            var session = sessionResult.Data!;
            vm.SessionId = session.Id;
            vm.OpenedAt = session.OpenedAt;
            vm.TerminalId = session.TerminalId;
            vm.OpeningCash = session.OpeningCash;

            var summaryResult = await posSessionManager.GetSessionSummaryAsync(session.Id);
            if (summaryResult.Success)
                vm.Summary = summaryResult.Data;
        }

        return View(vm);
    }

    // ── Session Management ───────────────────────────────────────────────

    [HttpPost("/pos/open-session")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenSession([FromForm] decimal openingCash, [FromForm] string? terminalId)
    {
        var dto = new OpenSessionDto(
            BranchOfficeId: DefaultBranchOfficeId,
            CashierId: GetCurrentUserId(),
            OpeningCash: openingCash,
            TerminalId: terminalId);

        var result = await posSessionManager.OpenSessionAsync(dto);

        if (!result.Success)
            TempData.SetError(result.Message ?? "Kasa açılamadı.");
        else
            TempData.SetSuccess("Kasa başarıyla açıldı.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/pos/close-session")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSession([FromForm] long sessionId, [FromForm] decimal closingCash)
    {
        var dto = new CloseSessionDto(SessionId: sessionId, ClosingCash: closingCash);
        var result = await posSessionManager.CloseSessionAsync(dto);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Kasa kapatılamadı.");
        }
        else
        {
            TempData.SetSuccess("Kasa başarıyla kapatıldı.");
            TempData["ZReportSessionId"] = sessionId.ToString(CultureInfo.InvariantCulture);
        }

        return RedirectToAction(nameof(Index));
    }

    // ── Product Search (HTMX) ────────────────────────────────────────────

    [HttpGet("/pos/search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Content("");

        var results = await productService.GetProductsDetailsPageable(
            new SearchablePageDto(q, 0, 10));

        if (!results.Success || results.Data?.Items is null || !results.Data.Items.Any())
            return PartialView("Partials/_POSSearchResults", new POSSearchResultsVm());

        return PartialView("Partials/_POSSearchResults", new POSSearchResultsVm
        {
            Products = results.Data.Items.Select(p => new POSSearchItemVm
            {
                ProductId = p.Id,
                Title = p.Title,
                StockCode = p.StockCode,
                BrandName = p.BrandName,
                ImageUrl = p.FeaturedImageUrl,
                CurrentStock = p.TotalCurrentStock
            }).ToList()
        });
    }

    // ── Cart Operations (HTMX) ──────────────────────────────────────────

    [HttpPost("/pos/add-item")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem([FromForm] Guid productId)
    {
        var cart = GetCartFromSession();

        // Urun detayini cek — fiyat ve varyant bilgisi icin
        var detailResult = await productService.GetProductDetailById(productId);
        if (!detailResult.Success || detailResult.Data is null)
        {
            Response.HtmxTriggerWithData("showToast", new { message = "Ürün bulunamadı.", level = "error" });
            return PartialView("Partials/_POSCart", cart);
        }

        var product = detailResult.Data;
        var firstVariant = product.ProductVariantsDetails?.FirstOrDefault();
        var variantId = firstVariant?.Id ?? Guid.Empty;

        var availableStock = variantId != Guid.Empty
            ? await officeStockManager.GetAvailableStockAsync(DefaultBranchOfficeId, variantId)
            : 0;

        var existingItem = cart.Items.FirstOrDefault(c => c.ProductId == productId);
        var desiredQuantity = existingItem?.Quantity + 1 ?? 1;

        if (desiredQuantity > availableStock)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = $"Stok yetersiz. Mevcut: {availableStock} adet.", level = "error" });
            // Mevcut cart aynen döner — quantity artırılmaz
            return PartialView("Partials/_POSCart", cart);
        }

        if (existingItem is not null)
        {
            existingItem.Quantity++;
            existingItem.AvailableStock = availableStock;
        }
        else
        {
            cart.Items.Add(new POSCartItemVm
            {
                ProductId = productId,
                VariantId = variantId,
                Title = product.Title,
                Barcode = firstVariant?.Barcode ?? "",
                ImageUrl = firstVariant?.imageLinks?.FirstOrDefault(),
                UnitPrice = firstVariant?.SalePrice ?? 0,
                ListPrice = firstVariant?.ListPrice ?? firstVariant?.SalePrice ?? 0,
                VatRate = firstVariant?.VatRate ?? 0,
                Quantity = 1,
                AvailableStock = availableStock
            });
        }

        SaveCartToSession(cart);
        return PartialView("Partials/_POSCart", cart);
    }

    [HttpPost("/pos/update-quantity")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity([FromForm] Guid productId, [FromForm] int quantity)
    {
        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(c => c.ProductId == productId);

        if (item is not null)
        {
            if (quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                var availableStock = await officeStockManager.GetAvailableStockAsync(
                    DefaultBranchOfficeId, item.VariantId);

                if (quantity > availableStock)
                {
                    Response.HtmxTriggerWithData("showToast",
                        new { message = $"Stok yetersiz. Mevcut: {availableStock} adet.", level = "error" });
                    item.AvailableStock = availableStock;
                }
                else
                {
                    item.Quantity = quantity;
                    item.AvailableStock = availableStock;
                }
            }
        }

        SaveCartToSession(cart);
        return PartialView("Partials/_POSCart", cart);
    }

    [HttpPost("/pos/remove-item")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveItem([FromForm] Guid productId)
    {
        var cart = GetCartFromSession();
        cart.Items.RemoveAll(c => c.ProductId == productId);
        SaveCartToSession(cart);
        return PartialView("Partials/_POSCart", cart);
    }

    [HttpPost("/pos/clear-cart")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearCart()
    {
        SaveCartToSession(new POSCartVm());
        HttpContext.Session.Remove("pos_customer_id");
        HttpContext.Session.Remove("pos_customer_name");
        return PartialView("Partials/_POSCart", new POSCartVm());
    }

    // ── Customer Search & Selection (HTMX) ─────────────────────────────

    [HttpGet("/pos/search-customer")]
    public async Task<IActionResult> SearchCustomer([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Content("");

        var result = await customerManager.GetCustomerBySearch(q);

        if (!result.Success || result.Data is null || !result.Data.Any())
            return PartialView("Partials/_POSCustomerResults", new SaleCustomerSearchVm());

        return PartialView("Partials/_POSCustomerResults", new SaleCustomerSearchVm
        {
            Customers = result.Data.Select(c => new SaleCustomerItemVm
            {
                Id = c.Id,
                Name = c.NameSurname ?? c.CorporateName ?? "-",
                Phone = c.PhoneNumber ?? "",
                Type = c.CustomerType ?? "",
                IsActive = c.IsActive
            }).ToList()
        });
    }

    [HttpPost("/pos/select-customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectCustomer([FromForm] int customerId, [FromForm] string customerName)
    {
        var customerResult = await customerManager.GetCustomerDetailById(customerId);
        if (!customerResult.Success || customerResult.Data == null)
        {
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = "Müşteri bulunamadı.", level = "error" });
            return NoContent();
        }
        if (!customerResult.Data.IsActive)
        {
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = "Deaktif müşteri seçilemez.", level = "error" });
            return NoContent();
        }

        HttpContext.Session.SetInt32("pos_customer_id", customerId);
        HttpContext.Session.SetString("pos_customer_name", customerName);
        return PartialView("Partials/_POSCustomerBadge", ((int?)customerId, customerName));
    }

    [HttpGet("/pos/quick-customer-dialog")]
    public IActionResult QuickCustomerDialog()
        => PartialView("Partials/_POSQuickCustomerDialog");

    [HttpPost("/pos/quick-customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCustomer(
        [FromForm] string name, [FromForm] string surname,
        [FromForm] string phone, [FromForm] string? nationalIdentity)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(surname))
        {
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = "Ad ve soyad zorunludur.", level = "error" });
            return NoContent();
        }

        var dto = new CustomerAddDto(
            NationalIdentity: nationalIdentity ?? "",
            TaxNumber: "",
            Name: name.Trim(),
            Surname: surname.Trim(),
            CorporateName: "",
            PhoneNumber: phone?.Trim() ?? "",
            FullAddress: "",
            CustomerType: "Retail");

        var result = await customerManager.AddCustomer(dto);
        if (!result.Success || result is not IDataResult<CustomerDetailDto> dataResult || dataResult.Data is null)
        {
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Müşteri eklenemedi.", level = "error" });
            return NoContent();
        }

        var fullName = $"{dto.Name} {dto.Surname}".Trim();
        HttpContext.Session.SetInt32("pos_customer_id", dataResult.Data.Id);
        HttpContext.Session.SetString("pos_customer_name", fullName);

        Response.HtmxTrigger("closePosQuickCustomerModal");
        Response.HtmxTriggerWithData("showToast",
            new { message = "Müşteri eklendi ve seçildi.", level = "success" });
        return PartialView("Partials/_POSCustomerBadge",
            ((int?)dataResult.Data.Id, (string?)fullName));
    }

    [HttpPost("/pos/clear-customer")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearCustomer()
    {
        HttpContext.Session.Remove("pos_customer_id");
        HttpContext.Session.Remove("pos_customer_name");
        return PartialView("Partials/_POSCustomerBadge", ((int?)null, (string?)null));
    }

    // ── Payment Dialog (HTMX) ───────────────────────────────────────────

    [HttpGet("/pos/payment-dialog")]
    public async Task<IActionResult> PaymentDialog()
    {
        var cart = GetCartFromSession();
        if (cart.Items.Count == 0)
        {
            // HTMX swap'i iptal et, stale modal açılmasın
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = "Sepet boş. Ödeme yapılamaz.", level = "error" });
            return NoContent();
        }

        var sessionResult = await posSessionManager.GetActiveSessionAsync(DefaultBranchOfficeId);
        if (!sessionResult.Success || sessionResult.Data is null)
        {
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = "Aktif kasa oturumu bulunamadı.", level = "error" });
            return NoContent();
        }

        var paymentMethodsResult = await paymentMethodManager.GetActivePaymentMethodsAsync(TenantId);

        // Idempotency — her PaymentDialog açılışında yeni token üret
        var submitToken = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("pos_submit_token", submitToken);

        var vm = new POSPaymentDialogVm
        {
            SessionId = sessionResult.Data.Id,
            SubtotalBeforeGeneralDiscount = cart.GrandTotalBeforeGeneralDiscount,
            GeneralDiscountAmount = cart.GeneralDiscountAmount,
            GeneralDiscountReasonName = cart.GeneralDiscountReasonName,
            Subtotal = cart.Subtotal,
            VatTotal = cart.VatTotal,
            GrandTotal = cart.GrandTotal,
            ItemCount = cart.TotalItems,
            PaymentMethods = paymentMethodsResult.Data ?? [],
            SubmitToken = submitToken
        };

        return PartialView("Partials/_POSPaymentDialog", vm);
    }

    // ── Close Session Dialog (HTMX) ─────────────────────────────────────

    [HttpGet("/pos/close-session-dialog")]
    public async Task<IActionResult> CloseSessionDialog([FromQuery] long sessionId)
    {
        var summaryResult = await posSessionManager.GetSessionSummaryAsync(sessionId);
        var summary = summaryResult.Data;

        var vm = new POSCloseSessionDialogVm
        {
            SessionId = sessionId,
            OpeningCash = summary?.OpeningCash ?? 0,
            TotalSales = summary?.TotalSales ?? 0,
            TotalCash = summary?.TotalCash ?? 0,
            TotalCard = summary?.TotalCard ?? 0,
            TransactionCount = summary?.TransactionCount ?? 0,
            ExpectedCash = summary?.ExpectedCash ?? 0
        };

        return PartialView("Partials/_POSCloseSessionDialog", vm);
    }

    // ── Kalem İndirimi (HTMX) ───────────────────────────────────────────

    [HttpGet("/pos/item-discount-dialog")]
    public async Task<IActionResult> ItemDiscountDialog([FromQuery] Guid variantId)
    {
        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(c => c.VariantId == variantId);
        if (item is null) return NotFound();

        var reasons = await discountReasonManager.GetActiveAsync();

        var vm = new POSLineDiscountDialogVm
        {
            VariantId = variantId,
            ProductTitle = item.Title,
            UnitPrice = item.UnitPrice,
            ListPrice = item.ListPrice,
            Quantity = item.Quantity,
            LineGross = item.LineGross,
            AlreadyDiscounted = item.ProductAlreadyDiscounted,
            CurrentPercent = item.DiscountPercent,
            CurrentAmount = item.DiscountAmount,
            CurrentReasonId = item.DiscountReasonId,
            CurrentNote = item.DiscountReasonNote,
            Reasons = reasons.ToList()
        };

        return PartialView("Partials/_POSLineDiscountModal", vm);
    }

    [HttpPost("/pos/apply-item-discount")]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyItemDiscount(
        [FromForm] Guid variantId,
        [FromForm] string discountType,        // "percent" | "amount"
        [FromForm] double? percent,
        [FromForm] decimal? amount,
        [FromForm] int? reasonId,
        [FromForm] string? note)
    {
        var cart = GetCartFromSession();
        var item = cart.Items.FirstOrDefault(c => c.VariantId == variantId);
        if (item is null)
        {
            Response.HtmxTriggerWithData("showToast", new { message = "Kalem bulunamadı.", level = "error" });
            return PartialView("Partials/_POSCart", cart);
        }

        // Reset
        item.DiscountPercent = 0;
        item.DiscountAmount = null;

        if (discountType == DiscountTypes.Percent && percent is > 0 and <= 100)
        {
            item.DiscountPercent = percent.Value;
        }
        else if (discountType == DiscountTypes.Amount && amount is > 0)
        {
            if (amount.Value > item.LineGross)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "TL indirimi satır toplamından büyük olamaz.", level = "error" });
                return PartialView("Partials/_POSCart", cart);
            }
            item.DiscountAmount = amount.Value;
        }

        item.DiscountReasonId = reasonId;
        item.DiscountReasonNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        SaveCartToSession(cart);

        Response.HtmxTriggerWithData("showToast",
            new { message = item.HasDiscount ? "İndirim uygulandı." : "İndirim kaldırıldı.", level = "success" });

        return PartialView("Partials/_POSCart", cart);
    }

    // ── Sepet (Genel) İndirimi (HTMX) ───────────────────────────────────

    [HttpGet("/pos/cart-discount-dialog")]
    public async Task<IActionResult> CartDiscountDialog()
    {
        var cart = GetCartFromSession();
        if (cart.Items.Count == 0)
        {
            Response.HtmxReswap("none");
            Response.HtmxTriggerWithData("showToast",
                new { message = "Sepet boş. İndirim uygulanamaz.", level = "error" });
            return NoContent();
        }

        var reasons = await discountReasonManager.GetActiveAsync();

        var vm = new POSCartDiscountDialogVm
        {
            SubtotalAfterLineDiscount = cart.SubtotalAfterLineDiscount,
            CurrentType = cart.GeneralDiscountType,
            CurrentValue = cart.GeneralDiscountValue,
            CurrentReasonId = cart.GeneralDiscountReasonId,
            CurrentNote = cart.GeneralDiscountReasonNote,
            Reasons = reasons.ToList()
        };

        return PartialView("Partials/_POSCartDiscountModal", vm);
    }

    [HttpPost("/pos/apply-cart-discount")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCartDiscount(
        [FromForm] string discountType,
        [FromForm] decimal? percent,
        [FromForm] decimal? amount,
        [FromForm] int? reasonId,
        [FromForm] string? note)
    {
        var cart = GetCartFromSession();
        if (cart.Items.Count == 0)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Sepet boş.", level = "error" });
            return PartialView("Partials/_POSCart", cart);
        }

        // Reset — validation başarısızsa SaveCartToSession çağrılmaz,
        // yani in-memory reset sadece view render için geçerlidir, session değişmez.
        cart.GeneralDiscountType = null;
        cart.GeneralDiscountValue = 0m;

        if (discountType == DiscountTypes.Percent)
        {
            if (percent is not > 0 || percent > 100m)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Yüzde 0'dan büyük ve 100'den küçük ya da eşit olmalı.", level = "error" });
                return PartialView("Partials/_POSCart", cart);
            }
            cart.GeneralDiscountType = DiscountTypes.Percent;
            cart.GeneralDiscountValue = percent.Value;
        }
        else if (discountType == DiscountTypes.Amount)
        {
            if (amount is not > 0m)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "İndirim 0'dan büyük olmalı.", level = "error" });
                return PartialView("Partials/_POSCart", cart);
            }
            if (amount.Value > cart.SubtotalAfterLineDiscount)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "İndirim sepet toplamından büyük olamaz.", level = "error" });
                return PartialView("Partials/_POSCart", cart);
            }
            cart.GeneralDiscountType = DiscountTypes.Amount;
            cart.GeneralDiscountValue = amount.Value;
        }
        else
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Geçersiz indirim tipi.", level = "error" });
            return PartialView("Partials/_POSCart", cart);
        }

        cart.GeneralDiscountReasonId = reasonId;
        cart.GeneralDiscountReasonNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        // Reason name denormalize
        if (reasonId.HasValue)
        {
            var reasons = await discountReasonManager.GetActiveAsync();
            cart.GeneralDiscountReasonName = reasons.FirstOrDefault(r => r.Id == reasonId.Value)?.Name;
        }
        else
        {
            cart.GeneralDiscountReasonName = null;
        }

        SaveCartToSession(cart);

        Response.HtmxTrigger("closePosCartDiscountModal");
        Response.HtmxTriggerWithData("showToast",
            new { message = "Sepet indirimi uygulandı.", level = "success" });

        return PartialView("Partials/_POSCart", cart);
    }

    [HttpPost("/pos/clear-cart-discount")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearCartDiscount()
    {
        var cart = GetCartFromSession();
        cart.GeneralDiscountType = null;
        cart.GeneralDiscountValue = 0m;
        cart.GeneralDiscountReasonId = null;
        cart.GeneralDiscountReasonName = null;
        cart.GeneralDiscountReasonNote = null;
        SaveCartToSession(cart);

        Response.HtmxTriggerWithData("showToast",
            new { message = "Sepet indirimi kaldırıldı.", level = "success" });

        return PartialView("Partials/_POSCart", cart);
    }

    // ── Complete Sale ────────────────────────────────────────────────────

    [HttpPost("/pos/complete-sale")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteSale([FromForm] List<SalePaymentDto> payments, [FromForm] string? submitToken)
    {
        // Idempotency — aynı token iki kez gelirse ikinci satış reddedilir
        const string tokenKey = "pos_submit_token";
        var expectedToken = HttpContext.Session.GetString(tokenKey);
        if (string.IsNullOrEmpty(submitToken) || expectedToken != submitToken)
        {
            TempData.SetError("Satış talebi yinelenmiş veya geçersiz. Lütfen tekrar deneyin.");
            return RedirectToAction(nameof(Index));
        }

        var cart = GetCartFromSession();
        if (cart.Items.Count == 0)
        {
            TempData.SetError("Sepet boş. Satış yapılamaz.");
            return RedirectToAction(nameof(Index));
        }

        if (payments == null || payments.Count == 0)
        {
            TempData.SetError("En az bir ödeme yöntemi seçmelisiniz.");
            return RedirectToAction(nameof(Index));
        }

        var sessionResult = await posSessionManager.GetActiveSessionAsync(DefaultBranchOfficeId);
        if (!sessionResult.Success || sessionResult.Data is null)
        {
            TempData.SetError("Aktif kasa oturumu bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        // Kalem indirimi uygulanmış satır bilgileri - pro-rata dağıtım için
        var lines = cart.Items.Select(c => new CartLine(
            UnitPrice: c.UnitPrice,
            Quantity: c.Quantity,
            VatRate: c.VatRate,
            ExistingLineDiscountAmount: c.DiscountAmount
                ?? Math.Round(c.LineGross * (decimal)c.DiscountPercent / 100m, 2)
        )).ToList();

        var generalDiscountGross = cart.GeneralDiscountAmount;
        var distribution = CartDiscountDistributor.Distribute(lines, generalDiscountGross);

        var saleItems = cart.Items.Select((c, idx) =>
        {
            var existingDiscount = c.DiscountAmount
                ?? Math.Round(c.LineGross * (decimal)c.DiscountPercent / 100m, 2);
            var newDiscountAmount = existingDiscount + distribution.PerLineNetShare[idx];

            return new SaleItemDto(
                ProductVariantId: c.VariantId,
                TaxPercentage: (double)c.VatRate,
                DiscountPercent: 0,                           // normalize: hepsi DiscountAmount'a
                UnitPrice: c.UnitPrice,
                Quantity: c.Quantity,
                DiscountVoucherCode: "",
                DiscountAmount: newDiscountAmount >= 0m
                    ? (newDiscountAmount > 0m ? newDiscountAmount : (decimal?)null)
                    : throw new InvalidOperationException($"Negatif indirim hesaplandı: {newDiscountAmount}"),
                DiscountReasonId: c.DiscountReasonId,
                DiscountReasonNote: c.DiscountReasonNote);
        }).ToList();

        var makeSaleDto = new MakeSaleDto(
            SalePersonId: GetCurrentUserId(),
            CustomerId: GetCustomerIdFromSession(),
            GeneralDiscount: distribution.AppliedGrossTotal,   // gerçek uygulanan (clamp sonrası)
            BranchOfficeId: DefaultBranchOfficeId,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: saleItems,
            Payments: payments,
            GeneralDiscountReasonId: cart.GeneralDiscountReasonId,
            GeneralDiscountReasonNote: cart.GeneralDiscountReasonNote);

        var saleResult = await saleManager.MakeSale(makeSaleDto);
        if (!saleResult.Success)
        {
            TempData.SetError(saleResult.Message ?? "Satış başarısız.");
            return RedirectToAction(nameof(Index));
        }

        // POSSession ↔ Sale bağlantı satırı — beklenen kasa hesaplaması bu satırdan geçer
        var cashReceived = payments.Sum(p => p.CashReceived ?? 0);
        var changeGiven  = payments.Sum(p => p.CashReceived.HasValue
            ? Math.Max(0, p.CashReceived.Value - p.Amount)
            : 0);
        await posSessionManager.AddTransactionRecordAsync(
            sessionResult.Data.Id, saleResult.Data, cashReceived, changeGiven);

        // Sepeti, müşteriyi ve submit token'ı temizle — yeni satışta taze token üretilir
        SaveCartToSession(new POSCartVm());
        HttpContext.Session.Remove("pos_customer_id");
        HttpContext.Session.Remove("pos_customer_name");
        HttpContext.Session.Remove(tokenKey);
        TempData.SetSuccess("Satış başarıyla tamamlandı.");
        return RedirectToAction(nameof(Index));
    }

    // ── Session-based Cart ───────────────────────────────────────────────

    private POSCartVm GetCartFromSession()
    {
        var json = HttpContext.Session.GetString("pos_cart");
        if (string.IsNullOrEmpty(json)) return new POSCartVm();

        // Yeni şema: POSCartVm (items + genel indirim state)
        try
        {
            var vm = System.Text.Json.JsonSerializer.Deserialize<POSCartVm>(json);
            if (vm is not null) return vm;
        }
        catch (System.Text.Json.JsonException)
        {
            // POSCartVm deserialize başarısız — eski liste şemasına düş
        }

        // Eski şema desteği: JSON bir liste ise items'a koy
        try
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<POSCartItemVm>>(json);
            if (items is not null) return new POSCartVm { Items = items };
        }
        catch (System.Text.Json.JsonException) { }

        return new POSCartVm();
    }

    private void SaveCartToSession(POSCartVm cart)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString("pos_cart", json);
    }

    // ── Reports ──────────────────────────────────────────────────────────

    [HttpGet("/pos/x-report")]
    public async Task<IActionResult> XReport()
    {
        var sessionResult = await posSessionManager.GetActiveSessionAsync(DefaultBranchOfficeId);
        if (!sessionResult.Success || sessionResult.Data is null)
        {
            TempData.SetError("Aktif kasa oturumu bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var reportResult = await posSessionManager.GetXReportAsync(sessionResult.Data.Id);
        if (!reportResult.Success || reportResult.Data is null)
        {
            TempData.SetError(reportResult.Message ?? "Rapor oluşturulamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("X Raporu");
        ViewData.SetActiveNav("pos");
        return View(reportResult.Data);
    }

    [HttpGet("/pos/z-report/{sessionId:long}")]
    public async Task<IActionResult> ZReport(long sessionId)
    {
        var reportResult = await posSessionManager.GetZReportAsync(sessionId);
        if (!reportResult.Success || reportResult.Data is null)
        {
            TempData.SetError(reportResult.Message ?? "Rapor oluşturulamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Z Raporu");
        ViewData.SetActiveNav("pos");
        return View(reportResult.Data);
    }
}
