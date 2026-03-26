using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Storefront;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class SeoController(
    IStorefrontTenantContext tenant,
    IProductService productService,
    ICategoryService categoryService) : Controller
{
    [Route("/robots.txt")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        _ = tenant; // Reserved for future tenant-scoped robots.txt
        var scheme = Request.Scheme;
        var host = Request.Host;

        var content = $"""
            User-agent: *
            Allow: /
            Disallow: /hesabim/
            Disallow: /sepet
            Disallow: /odeme

            Sitemap: {scheme}://{host}/sitemap.xml
            """;

        return Content(content, "text/plain");
    }

    [Route("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        var scheme = Request.Scheme;
        var host = Request.Host;
        var baseUrl = $"{scheme}://{host}";

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Static pages
        AppendUrl(sb, $"{baseUrl}/", "daily", "1.0");
        AppendUrl(sb, $"{baseUrl}/hakkimizda", "monthly", "0.5");
        AppendUrl(sb, $"{baseUrl}/iletisim", "monthly", "0.5");
        AppendUrl(sb, $"{baseUrl}/kategoriler", "weekly", "0.7");

        // Dynamic categories
        var categoriesResult = await categoryService.GetCategoryTreeAsync();
        if (categoriesResult.Success && categoriesResult.Data is not null)
        {
            AppendCategoryUrls(sb, baseUrl, categoriesResult.Data, null);
        }

        // Dynamic products
        var productsResult = await productService.GetStorefrontProductsAsync(
            new StorefrontCatalogQuery(PageSize: 50000));
        if (productsResult.Success && productsResult.Data is not null)
        {
            foreach (var product in productsResult.Data.Items)
            {
                if (!string.IsNullOrWhiteSpace(product.SeoSlug))
                {
                    AppendUrl(sb, $"{baseUrl}/urun/{product.SeoSlug}", "weekly", "0.8");
                }
            }
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml");
    }

    private static void AppendUrl(StringBuilder sb, string loc, string changefreq, string priority)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{loc}</loc>");
        sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine("  </url>");
    }

    private static void AppendCategoryUrls(StringBuilder sb, string baseUrl,
        List<CategoryTreeDto> categories, string? parentSlug)
    {
        foreach (var cat in categories)
        {
            if (!string.IsNullOrWhiteSpace(cat.SeoSlug))
            {
                var url = parentSlug != null
                    ? $"{baseUrl}/kategori/{parentSlug}/{cat.SeoSlug}"
                    : $"{baseUrl}/kategori/{cat.SeoSlug}";
                AppendUrl(sb, url, "weekly", "0.7");

                if (cat.Children.Count > 0)
                {
                    AppendCategoryUrls(sb, baseUrl, cat.Children, cat.SeoSlug);
                }
            }
        }
    }
}
