using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

[Authorize]
public class AccountController(
    IStorefrontTenantContext tenant,
    IStorefrontAuthManager authManager) : Controller
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

    public IActionResult Orders()
    {
        ViewBag.PageTitle = "Siparislerim";
        return View();
    }

    public IActionResult Addresses()
    {
        ViewBag.PageTitle = "Adreslerim";
        return View();
    }
}
