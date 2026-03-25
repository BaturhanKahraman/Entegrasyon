using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Storefront.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class ProductController(
    IStorefrontTenantContext tenant,
    IProductService productService) : Controller
{
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var result = await productService.GetStorefrontProductDetailAsync(slug);
        if (!result.Success || result.Data is null)
            return NotFound();

        var product = result.Data;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        ViewData["JsonLd"] = JsonLdBuilder.BuildProduct(product, baseUrl);

        // Fetch related products (same category, excluding current)
        var relatedQuery = new StorefrontCatalogQuery(CategoryId: product.CategoryId, PageSize: 9);
        var relatedResult = await productService.GetStorefrontProductsAsync(relatedQuery);
        var relatedProducts = relatedResult.Success
            ? relatedResult.Data.Items
                .Where(p => p.Id != product.Id)
                .Take(8)
                .ToList()
            : new List<StorefrontProductCardDto>();

        ViewBag.RelatedProducts = relatedProducts;
        ViewBag.SeoTitle = product.SeoTitle ?? $"{product.Title} - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = product.SeoDescription
            ?? $"{product.Title} - {tenant.Settings.StoreName}";
        ViewBag.Breadcrumbs = product.Breadcrumbs;

        return View(product);
    }
}
