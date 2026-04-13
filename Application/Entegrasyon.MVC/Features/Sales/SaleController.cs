using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Entegrasyon.MVC.Features.Sales.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Sales;

[Authorize]
public class SaleController(
    ISaleManager saleManager,
    IProductService productService,
    ICustomerManager customerManager,
    IPaymentMethodManager paymentMethodManager,
    ITenantContext tenantContext) : Controller
{
    private const int defaultBranchOfficeId = 1;
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Sale List ────────────────────────────────────────────────────────

    [HttpGet("/sales")]
    public async Task<IActionResult> Index(
        string? search = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        SaleSource? source = null,
        SaleStatus? status = null,
        int page = 0)
    {
        var start = startDate ?? DateTimeOffset.UtcNow.Date;
        var end = endDate ?? DateTimeOffset.UtcNow.Date.AddDays(1);

        var dto = new SalePageableDto(
            CustomerId: null,
            DateBetweenStart: start,
            DateBetweenEnd: end,
            SalePersonId: Guid.Empty,
            SaleSource: source,
            SaleStatus: status,
            FullTextSearchKey: search ?? "",
            PageIndex: page);

        var salesResult = await saleManager.GetSalesPageable(dto);
        var summaryResult = await saleManager.GetSalesSummaryAsync(dto);

        ViewData.SetPageTitle("Satışlar");
        ViewData.SetActiveNav("sales");
        ViewBag.Summary = summaryResult.Data;
        ViewBag.CurrentFilters = dto;

        if (Request.IsHtmx())
            return PartialView("Partials/_SaleTable", salesResult.Data);

        return View(salesResult.Data);
    }

    // ── New Sale Entry ───────────────────────────────────────────────────

    [HttpGet("/sales/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Satış");
        ViewData.SetActiveNav("sales");

        var cart = GetCartFromSession();
        var vm = new SaleEntryVm { Items = cart };

        // Restore customer from session
        var custId = HttpContext.Session.GetInt32("sale_customer_id");
        var custName = HttpContext.Session.GetString("sale_customer_name");
        if (custId.HasValue && !string.IsNullOrEmpty(custName))
        {
            vm.CustomerId = custId.Value;
            vm.CustomerName = custName;
        }

        return View(vm);
    }

    // ── Product Search (HTMX) ────────────────────────────────────────────

    [HttpGet("/sales/search-product")]
    public async Task<IActionResult> SearchProduct([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Content("");

        var results = await productService.GetProductsDetailsPageable(
            new SearchablePageDto(q, 0, 10));

        if (!results.Success || results.Data?.Items is null || !results.Data.Items.Any())
            return PartialView("Partials/_SaleSearchResults", new SaleSearchResultsVm());

        return PartialView("Partials/_SaleSearchResults", new SaleSearchResultsVm
        {
            Products = results.Data.Items.Select(p => new SaleSearchItemVm
            {
                ProductId = p.Id,
                Title = p.Title,
                StockCode = p.StockCode,
                BrandName = p.BrandName,
                CurrentStock = p.TotalCurrentStock
            }).ToList()
        });
    }

    // ── Customer Search (HTMX) ──────────────────────────────────────────

    [HttpGet("/sales/search-customer")]
    public async Task<IActionResult> SearchCustomer([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Content("");

        var result = await customerManager.GetCustomerBySearch(q);

        if (!result.Success || result.Data is null || !result.Data.Any())
            return PartialView("Partials/_SaleCustomerResults", new SaleCustomerSearchVm());

        return PartialView("Partials/_SaleCustomerResults", new SaleCustomerSearchVm
        {
            Customers = result.Data.Select(c => new SaleCustomerItemVm
            {
                Id = c.Id,
                Name = c.NameSurname ?? c.CorporateName ?? "-",
                Phone = c.PhoneNumber ?? "",
                Type = c.CustomerType ?? ""
            }).ToList()
        });
    }

    // ── Cart Operations (HTMX) ──────────────────────────────────────────

    [HttpPost("/sales/add-item")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem([FromForm] Guid productId)
    {
        var cart = GetCartFromSession();

        var detailResult = await productService.GetProductDetailById(productId);
        if (!detailResult.Success || detailResult.Data is null)
            return PartialView("Partials/_SaleCart", new SaleEntryVm { Items = cart });

        var product = detailResult.Data;
        var firstVariant = product.ProductVariantsDetails?.FirstOrDefault();

        var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
        if (existingItem is not null)
        {
            existingItem.Quantity++;
        }
        else
        {
            cart.Add(new SaleCartItemVm
            {
                ProductId = productId,
                VariantId = firstVariant?.Id ?? Guid.Empty,
                Title = product.Title,
                Barcode = firstVariant?.Barcode ?? "",
                BrandName = product.BrandName,
                UnitPrice = firstVariant?.SalePrice ?? 0,
                VatRate = firstVariant?.VatRate ?? 0,
                Quantity = 1
            });
        }

        SaveCartToSession(cart);
        return PartialView("Partials/_SaleCart", new SaleEntryVm { Items = cart });
    }

    [HttpPost("/sales/update-quantity")]
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
        return PartialView("Partials/_SaleCart", new SaleEntryVm { Items = cart });
    }

    [HttpPost("/sales/remove-item")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveItem([FromForm] Guid productId)
    {
        var cart = GetCartFromSession();
        cart.RemoveAll(c => c.ProductId == productId);
        SaveCartToSession(cart);
        return PartialView("Partials/_SaleCart", new SaleEntryVm { Items = cart });
    }

    [HttpPost("/sales/clear-cart")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearCart()
    {
        SaveCartToSession([]);
        HttpContext.Session.Remove("sale_customer_id");
        HttpContext.Session.Remove("sale_customer_name");
        return PartialView("Partials/_SaleCart", new SaleEntryVm());
    }

    // ── Customer Selection (HTMX) ───────────────────────────────────────

    [HttpPost("/sales/select-customer")]
    [ValidateAntiForgeryToken]
    public IActionResult SelectCustomer([FromForm] int customerId, [FromForm] string customerName)
    {
        HttpContext.Session.SetInt32("sale_customer_id", customerId);
        HttpContext.Session.SetString("sale_customer_name", customerName);

        var cart = GetCartFromSession();
        var vm = new SaleEntryVm
        {
            Items = cart,
            CustomerId = customerId,
            CustomerName = customerName
        };

        return PartialView("Partials/_SaleCustomerBadge", vm);
    }

    [HttpPost("/sales/clear-customer")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearCustomer()
    {
        HttpContext.Session.Remove("sale_customer_id");
        HttpContext.Session.Remove("sale_customer_name");

        return PartialView("Partials/_SaleCustomerBadge", new SaleEntryVm());
    }

    // ── Complete Sale ────────────────────────────────────────────────────

    [HttpPost("/sales/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteSale()
    {
        var cart = GetCartFromSession();
        if (cart.Count == 0)
        {
            TempData.SetError("Sepet bos. Satis yapilamaz.");
            return RedirectToAction(nameof(Create));
        }

        var customerId = HttpContext.Session.GetInt32("sale_customer_id") ?? 0;

        var saleItems = cart.Select(c => new SaleItemDto(
            ProductVariantId: c.VariantId,
            TaxPercentage: (double)c.VatRate,
            DiscountPercent: 0,
            UnitPrice: c.UnitPrice,
            Quantity: c.Quantity,
            DiscountVoucherCode: "")).ToList();

        var makeSaleDto = new MakeSaleDto(
            SalePersonId: GetCurrentUserId(),
            CustomerId: customerId,
            GeneralDiscount: 0,
            BranchOfficeId: defaultBranchOfficeId,
            SaleSource: SaleSource.POS,
            Note: null,
            SaleItems: saleItems,
            Payments: []);

        var result = await saleManager.MakeSale(makeSaleDto);

        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Satis basarisiz.");
            return RedirectToAction(nameof(Create));
        }

        // Temizle
        SaveCartToSession([]);
        HttpContext.Session.Remove("sale_customer_id");
        HttpContext.Session.Remove("sale_customer_name");

        TempData.SetSuccess("Satis basariyla tamamlandi.");
        return RedirectToAction(nameof(Index));
    }

    // ── Sale Detail ──────────────────────────────────────────────────────

    [HttpGet("/sales/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await saleManager.GetSaleDetailAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData.SetError(result.Message ?? "Satış bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var paymentMethods = await paymentMethodManager.GetActivePaymentMethodsAsync(TenantId);

        ViewData.SetPageTitle($"Satış Detay — {result.Data.SaleNumber}");
        ViewData.SetActiveNav("sales");
        ViewBag.PaymentMethods = paymentMethods.Data ?? [];

        return View(result.Data);
    }

    [HttpGet("/sales/{id:guid}/print")]
    public async Task<IActionResult> Print(Guid id)
    {
        var result = await saleManager.GetSaleDetailAsync(id);
        if (!result.Success || result.Data is null)
            return NotFound();

        return View(result.Data);
    }

    [HttpPost("/sales/{id:guid}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await saleManager.CancelSaleAsync(id, userId);

        if (result.Success)
            TempData.SetSuccess(result.Message ?? "Satış iptal edildi.");
        else
            TempData.SetError(result.Message ?? "Satış iptal edilemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── Session-based Cart ───────────────────────────────────────────────

    private List<SaleCartItemVm> GetCartFromSession()
    {
        var json = HttpContext.Session.GetString("sale_cart");
        if (string.IsNullOrEmpty(json)) return [];
        return System.Text.Json.JsonSerializer.Deserialize<List<SaleCartItemVm>>(json) ?? [];
    }

    private void SaveCartToSession(List<SaleCartItemVm> cart)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString("sale_cart", json);
    }
}
