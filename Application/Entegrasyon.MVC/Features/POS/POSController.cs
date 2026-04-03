using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.POS;
using Entegrasyon.MVC.Features.POS.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.POS;

[Authorize]
public class POSController(
    IPOSSessionManager posSessionManager,
    ISaleManager saleManager,
    IProductService productService) : Controller
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string GetCurrentUserName()
        => User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name) ?? "Kasiyer";

    // Varsayilan branch office — ilerde claims'ten alinabilir
    private const int DefaultBranchOfficeId = 1;

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
            HasActiveSession = sessionResult.Success && sessionResult.Data is not null
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
            TempData.SetError(result.Message ?? "Kasa acilamadi.");
        else
            TempData.SetSuccess("Kasa basariyla acildi.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/pos/close-session")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSession([FromForm] long sessionId, [FromForm] decimal closingCash)
    {
        var dto = new CloseSessionDto(SessionId: sessionId, ClosingCash: closingCash);
        var result = await posSessionManager.CloseSessionAsync(dto);

        if (!result.Success)
            TempData.SetError(result.Message ?? "Kasa kapatilamadi.");
        else
            TempData.SetSuccess("Kasa basariyla kapatildi.");

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
            Response.HtmxTrigger("showToast");
            return PartialView("Partials/_POSCart", new POSCartVm { Items = cart });
        }

        var product = detailResult.Data;
        var firstVariant = product.ProductVariantsDetails?.FirstOrDefault();

        var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
        if (existingItem is not null)
        {
            existingItem.Quantity++;
        }
        else
        {
            cart.Add(new POSCartItemVm
            {
                ProductId = productId,
                VariantId = firstVariant?.Id ?? Guid.Empty,
                Title = product.Title,
                Barcode = firstVariant?.Barcode ?? "",
                UnitPrice = firstVariant?.SalePrice ?? 0,
                VatRate = firstVariant?.VatRate ?? 0,
                Quantity = 1
            });
        }

        SaveCartToSession(cart);
        return PartialView("Partials/_POSCart", new POSCartVm { Items = cart });
    }

    [HttpPost("/pos/update-quantity")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateQuantity([FromForm] Guid productId, [FromForm] int quantity)
    {
        var cart = GetCartFromSession();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);

        if (item is not null)
        {
            if (quantity <= 0)
                cart.Remove(item);
            else
                item.Quantity = quantity;
        }

        SaveCartToSession(cart);
        return PartialView("Partials/_POSCart", new POSCartVm { Items = cart });
    }

    [HttpPost("/pos/remove-item")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveItem([FromForm] Guid productId)
    {
        var cart = GetCartFromSession();
        cart.RemoveAll(c => c.ProductId == productId);
        SaveCartToSession(cart);
        return PartialView("Partials/_POSCart", new POSCartVm { Items = cart });
    }

    [HttpPost("/pos/clear-cart")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearCart()
    {
        SaveCartToSession([]);
        return PartialView("Partials/_POSCart", new POSCartVm { Items = [] });
    }

    // ── Payment Dialog (HTMX) ───────────────────────────────────────────

    [HttpGet("/pos/payment-dialog")]
    public IActionResult PaymentDialog([FromQuery] long sessionId)
    {
        var cart = GetCartFromSession();
        var cartVm = new POSCartVm { Items = cart };

        var vm = new POSPaymentDialogVm
        {
            SessionId = sessionId,
            Subtotal = cartVm.Subtotal,
            VatTotal = cartVm.VatTotal,
            GrandTotal = cartVm.GrandTotal,
            ItemCount = cartVm.TotalItems
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

    // ── Complete Sale ────────────────────────────────────────────────────

    [HttpPost("/pos/complete-sale")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteSale(
        [FromForm] long sessionId,
        [FromForm] PaymentMethod paymentMethod,
        [FromForm] decimal cashReceived,
        [FromForm] string? cardAuthCode)
    {
        var cart = GetCartFromSession();
        if (cart.Count == 0)
        {
            TempData.SetError("Sepet bos. Satis yapilamaz.");
            return RedirectToAction(nameof(Index));
        }

        var saleItems = cart.Select(c => new SaleItemDto(
            ProductVariantId: c.VariantId,
            TaxPercentage: (double)c.VatRate,
            DiscountPercent: 0,
            UnitPrice: c.UnitPrice,
            Quantity: c.Quantity,
            DiscountVoucherCode: "")).ToList();

        var makeSaleDto = new MakeSaleDto(
            SalePersonId: GetCurrentUserId(),
            CustomerId: 0,
            GeneralDiscount: 0,
            BranchOfficeId: DefaultBranchOfficeId,
            SaleItems: saleItems);

        // Satis kaydi
        var saleResult = await saleManager.MakeSale(makeSaleDto);
        if (!saleResult.Success)
        {
            TempData.SetError(saleResult.Message ?? "Satis basarisiz.");
            return RedirectToAction(nameof(Index));
        }

        // POS Transaction kaydi
        var transactionDto = new POSTransactionDto(
            POSSessionId: sessionId,
            Sale: makeSaleDto,
            PaymentMethod: paymentMethod,
            CashReceived: cashReceived,
            CardAuthCode: cardAuthCode);

        await posSessionManager.RecordTransactionAsync(transactionDto);

        // Sepeti temizle
        SaveCartToSession([]);
        TempData.SetSuccess("Satis basariyla tamamlandi.");
        return RedirectToAction(nameof(Index));
    }

    // ── Session-based Cart ───────────────────────────────────────────────

    private List<POSCartItemVm> GetCartFromSession()
    {
        var json = HttpContext.Session.GetString("pos_cart");
        if (string.IsNullOrEmpty(json)) return [];
        return System.Text.Json.JsonSerializer.Deserialize<List<POSCartItemVm>>(json) ?? [];
    }

    private void SaveCartToSession(List<POSCartItemVm> cart)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString("pos_cart", json);
    }
}
