using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Entegrasyon.Storefront.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class HomeController(
    IStorefrontTenantContext tenant,
    IStorefrontBannerManager bannerManager,
    IProductService productService,
    ICategoryService categoryService) : Controller
{
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> Index()
    {
        var bannersResult = await bannerManager.GetActiveBannersAsync(tenant.TenantId, BannerPosition.Hero);
        var banners = bannersResult.Success ? bannersResult.Data : new List<StorefrontBanner>();

        var newProductsResult = await productService.GetNewProductsAsync(8);
        var bestSellersResult = await productService.GetBestSellersAsync(8);
        var topCategoriesResult = await categoryService.GetCategoryTreeAsync();

        ViewData["JsonLd"] = JsonLdBuilder.BuildStore(tenant.Settings);
        ViewBag.Banners = banners;
        ViewBag.NewProducts = newProductsResult.Success
            ? newProductsResult.Data
            : new List<StorefrontProductCardDto>();
        ViewBag.BestSellers = bestSellersResult.Success
            ? bestSellersResult.Data
            : new List<StorefrontProductCardDto>();
        ViewBag.TopCategories = topCategoriesResult.Success
            ? topCategoriesResult.Data
            : new List<CategoryTreeDto>();
        ViewBag.SeoTitle = tenant.Settings.DefaultSeoTitle ?? tenant.Settings.StoreName;
        ViewBag.SeoDescription = tenant.Settings.DefaultSeoDescription ?? tenant.Settings.StoreSlogan;

        return View();
    }
}
