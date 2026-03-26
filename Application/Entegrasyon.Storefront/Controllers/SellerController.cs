using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class SellerController(
    IStorefrontTenantContext tenant,
    ISellerManager sellerManager) : Controller
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Register()
    {
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
        var seller = await GetApprovedSellerAsync();
        if (seller is null) return RedirectToAction("Register");

        ViewBag.Seller = seller;
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
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
        var customerId = GetCustomerId();
        if (customerId is null) return RedirectToAction("Login", "Auth");

        var existing = await sellerManager.GetSellerByCustomerIdAsync(tenant.TenantId, customerId.Value);
        if (!existing.Success) return RedirectToAction("Register");

        if (existing.Data.Status == SellerStatus.Approved)
            return RedirectToAction("Panel");

        ViewBag.Seller = existing.Data;
        return View();
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
