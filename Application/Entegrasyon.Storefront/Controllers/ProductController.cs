using System.Security.Claims;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Storefront.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class ProductController(
    IStorefrontTenantContext tenant,
    IProductService productService,
    IStorefrontReviewManager reviewManager) : Controller
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

        // Fetch reviews and rating
        var reviewsResult = await reviewManager.GetProductReviewsAsync(tenant.TenantId, product.Id);
        var ratingResult = await reviewManager.GetProductRatingAsync(tenant.TenantId, product.Id);

        ViewBag.Reviews = reviewsResult.Success ? reviewsResult.Data : new List<Entity.Storefront.StorefrontReview>();
        ViewBag.AverageRating = ratingResult.Success ? ratingResult.Data.AverageRating : 0.0;
        ViewBag.ReviewCount = ratingResult.Success ? ratingResult.Data.ReviewCount : 0;

        ViewBag.RelatedProducts = relatedProducts;
        ViewBag.SeoTitle = product.SeoTitle ?? $"{product.Title} - {tenant.Settings.StoreName}";
        ViewBag.SeoDescription = product.SeoDescription
            ?? $"{product.Title} - {tenant.Settings.StoreName}";
        ViewBag.Breadcrumbs = product.Breadcrumbs;

        return View(product);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddReview(string slug, int rating, string comment, string? title)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var productResult = await productService.GetStorefrontProductDetailAsync(slug);
        if (!productResult.Success || productResult.Data is null)
            return NotFound();

        var customerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await reviewManager.AddReviewAsync(
            tenant.TenantId, productResult.Data.Id, customerId, rating, comment, title);

        TempData[result.Success ? "ReviewSuccess" : "ReviewError"] = result.Message;
        return Redirect($"/urun/{slug}#reviews");
    }
}
