using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Pricing;

[Authorize]
public class PricingController(IProductService productService) : HtmxController
{
    [HttpGet("/pricing")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Fiyat Yonetimi");
        ViewData.SetActiveNav("pricing");
        ViewData.SetBreadcrumb(("Fiyat Yonetimi", null));

        var result = await productService.GetProductsDetailsPageable(
            new SearchablePageDto(search ?? "", page - 1, 30));

        ViewBag.Search = search;
        return HtmxView(result.Data);
    }

    [HttpGet("/pricing/rules")]
    public IActionResult Rules()
    {
        ViewData.SetPageTitle("Fiyatlandirma Kurallari");
        ViewData.SetActiveNav("pricing-rules");
        ViewData.SetBreadcrumb(("Fiyat Yonetimi", "/pricing"), ("Kurallar", null));

        return View();
    }
}
