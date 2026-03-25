using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class SeoController(IStorefrontTenantContext tenant) : Controller
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
    public IActionResult Sitemap()
    {
        var scheme = Request.Scheme;
        var host = Request.Host;
        var baseUrl = $"{scheme}://{host}";

        var xml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
              <url>
                <loc>{baseUrl}/</loc>
                <changefreq>daily</changefreq>
                <priority>1.0</priority>
              </url>
              <url>
                <loc>{baseUrl}/hakkimizda</loc>
                <changefreq>monthly</changefreq>
                <priority>0.5</priority>
              </url>
              <url>
                <loc>{baseUrl}/iletisim</loc>
                <changefreq>monthly</changefreq>
                <priority>0.5</priority>
              </url>
            </urlset>
            """;

        return Content(xml, "application/xml");
    }
}
