using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class ContactController(
    IStorefrontTenantContext tenant,
    IStorefrontContactManager contactManager) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.SeoTitle = $"Iletisim | {tenant.Settings.StoreName}";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string name, string email, string? phone, string? subject, string message)
    {
        var result = await contactManager.SubmitMessageAsync(tenant.TenantId, name, email, phone, subject, message);
        ViewBag.Success = result.Success;
        ViewBag.Message = result.Message;
        ViewBag.SeoTitle = $"Iletisim | {tenant.Settings.StoreName}";
        return View();
    }
}
