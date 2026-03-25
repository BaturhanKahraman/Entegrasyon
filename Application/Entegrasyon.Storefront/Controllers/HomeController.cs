using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Entegrasyon.Storefront.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class HomeController(
    IStorefrontTenantContext tenant,
    IStorefrontBannerManager bannerManager) : Controller
{
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> Index()
    {
        var bannersResult = await bannerManager.GetActiveBannersAsync(tenant.TenantId, BannerPosition.Hero);
        var banners = bannersResult.Success ? bannersResult.Data : new List<StorefrontBanner>();

        ViewData["JsonLd"] = JsonLdBuilder.BuildStore(tenant.Settings);
        ViewBag.Banners = banners;
        ViewBag.SeoTitle = tenant.Settings.DefaultSeoTitle ?? tenant.Settings.StoreName;
        ViewBag.SeoDescription = tenant.Settings.DefaultSeoDescription ?? tenant.Settings.StoreSlogan;

        return View();
    }
}
