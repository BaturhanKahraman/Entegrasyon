using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Storefront.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class CompareController(
    IStorefrontTenantContext tenant,
    IProductService productService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] string? ids)
    {
        ViewBag.SeoTitle = $"Urun Karsilastirma - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = "Urunleri yan yana karsilastirin.";
        ViewBag.Breadcrumbs = new List<BreadcrumbItemDto>
        {
            new("Ana Sayfa", "/"),
            new("Karsilastir", "/karsilastir")
        };

        var products = new List<StorefrontProductDetailDto>();

        if (!string.IsNullOrWhiteSpace(ids))
        {
            var guidIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .Take(4)
                .ToList();

            if (guidIds.Count > 0)
            {
                var result = await productService.GetProductsByIdsAsync(guidIds);
                if (result.Success)
                    products = result.Data;
            }
        }

        return View(products);
    }
}
