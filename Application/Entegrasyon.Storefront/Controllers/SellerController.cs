using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class SellerController(
    IStorefrontTenantContext tenant,
    ISellerManager sellerManager,
    ISellerOrderManager sellerOrderManager,
    ISellerPayoutManager sellerPayoutManager) : Controller
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var customerId = GetCustomerId();
        if (customerId is null) return RedirectToAction("Login", "Auth");

        // Check if already a seller
        var existing = await sellerManager.GetSellerByCustomerIdAsync(tenant.TenantId, customerId.Value);
        if (existing.Success)
        {
            return existing.Data.Status switch
            {
                SellerStatus.Pending => RedirectToAction("Pending"),
                SellerStatus.Approved => RedirectToAction("Panel"),
                SellerStatus.Suspended => RedirectToAction("Pending"),
                _ => View()
            };
        }

        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(SellerRegistrationDto dto)
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var customerId = GetCustomerId();
        if (customerId is null) return RedirectToAction("Login", "Auth");

        var result = await sellerManager.RegisterSellerAsync(tenant.TenantId, customerId.Value, dto);
        if (!result.Success)
        {
            ViewBag.Error = result.Message;
            return View();
        }

        return RedirectToAction("Pending");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Panel()
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register");

        ViewBag.Seller = seller;
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register");

        ViewBag.Seller = seller;
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(SellerProfileDto dto)
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register");

        var result = await sellerManager.UpdateSellerProfileAsync(seller.Id, dto);
        ViewBag.Seller = seller;
        ViewBag.Success = result.Success;
        ViewBag.Error = result.Success ? null : result.Message;
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Pending()
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var customerId = GetCustomerId();
        if (customerId is null) return RedirectToAction("Login", "Auth");

        var existing = await sellerManager.GetSellerByCustomerIdAsync(tenant.TenantId, customerId.Value);
        if (!existing.Success) return RedirectToAction("Register");

        if (existing.Data.Status == SellerStatus.Approved)
            return RedirectToAction("Panel");

        ViewBag.Seller = existing.Data;
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register");

        var result = await sellerOrderManager.GetSellerOrdersAsync(seller.Id);
        ViewBag.Seller = seller;
        ViewBag.Orders = result.Success ? result.Data : new List<Entity.Orders.Order>();
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> OrderDetail(Guid id)
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register");

        var result = await sellerOrderManager.GetSellerOrderDetailAsync(seller.Id, id);
        if (!result.Success)
            return NotFound();

        ViewBag.Seller = seller;
        return View(result.Data);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Balance()
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register");

        var balanceResult = await sellerPayoutManager.GetBalanceAsync(seller.Id);
        var transactionsResult = await sellerPayoutManager.GetTransactionsAsync(seller.Id);
        var payoutsResult = await sellerPayoutManager.GetPayoutRequestsAsync(seller.Id);

        ViewBag.Seller = seller;
        ViewBag.Balance = balanceResult.Success ? balanceResult.Data : null;
        ViewBag.Transactions = transactionsResult.Success ? transactionsResult.Data : new List<SellerTransaction>();
        ViewBag.Payouts = payoutsResult.Success ? payoutsResult.Data : new List<PayoutRequest>();
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayout(decimal amount)
    {
        if (!tenant.Settings.MarketplaceEnabled) return NotFound();

        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register");

        var result = await sellerPayoutManager.RequestPayoutAsync(seller.Id, amount);
        TempData["PayoutMessage"] = result.Message;
        TempData["PayoutSuccess"] = result.Success;
        return RedirectToAction("Balance");
    }

    private int? GetCustomerId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private async Task<Seller?> GetApprovedSellerAsync()
    {
        var customerId = GetCustomerId();
        if (customerId is null) return null;

        var result = await sellerManager.GetSellerByCustomerIdAsync(tenant.TenantId, customerId.Value);
        if (!result.Success || result.Data.Status != SellerStatus.Approved) return null;

        return result.Data;
    }
}
