using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Storefront;

[Authorize]
public class StorefrontController : Controller
{
    [HttpGet("/settings/storefront")]
    public IActionResult Settings()
    {
        ViewData.SetPageTitle("Magaza Ayarlari");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", null), ("Ayarlar", null));
        return View();
    }

    [HttpGet("/settings/storefront/banners")]
    public IActionResult Banners()
    {
        ViewData.SetPageTitle("Banner Yonetimi");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Bannerlar", null));
        return View();
    }

    [HttpGet("/storefront/campaigns")]
    public IActionResult Campaigns()
    {
        ViewData.SetPageTitle("Kampanyalar");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Kampanyalar", null));
        return View();
    }

    [HttpGet("/storefront/reviews")]
    public IActionResult Reviews()
    {
        ViewData.SetPageTitle("Degerlendirmeler");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Degerlendirmeler", null));
        return View();
    }

    [HttpGet("/storefront/returns")]
    public IActionResult Returns()
    {
        ViewData.SetPageTitle("Iadeler");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Iadeler", null));
        return View();
    }
}
