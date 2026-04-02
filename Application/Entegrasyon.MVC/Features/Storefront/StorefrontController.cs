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

    [HttpGet("/settings/storefront/legal")]
    public IActionResult Legal()
    {
        ViewData.SetPageTitle("Yasal Bilgiler");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Yasal Bilgiler", null));
        return View();
    }

    [HttpGet("/settings/storefront/payment")]
    public IActionResult Payment()
    {
        ViewData.SetPageTitle("Odeme Ayarlari");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Odeme Ayarlari", null));
        return View();
    }

    [HttpGet("/storefront/email-campaigns")]
    public IActionResult EmailCampaigns()
    {
        ViewData.SetPageTitle("E-posta Kampanyalari");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("E-posta Kampanyalari", null));
        return View();
    }

    [HttpGet("/storefront/messages")]
    public IActionResult Messages()
    {
        ViewData.SetPageTitle("Mesajlar");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Mesajlar", null));
        return View();
    }

    [HttpGet("/storefront/newsletter")]
    public IActionResult Newsletter()
    {
        ViewData.SetPageTitle("Bulten Yonetimi");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Bulten", null));
        return View();
    }

    [HttpGet("/storefront/payouts")]
    public IActionResult Payouts()
    {
        ViewData.SetPageTitle("Odemeler");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Odemeler", null));
        return View();
    }

    [HttpGet("/storefront/sellers")]
    public IActionResult Sellers()
    {
        ViewData.SetPageTitle("Saticilar");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Saticilar", null));
        return View();
    }

    [HttpGet("/storefront/size-guides")]
    public IActionResult SizeGuides()
    {
        ViewData.SetPageTitle("Beden Kilavuzlari");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Beden Kilavuzlari", null));
        return View();
    }
}
