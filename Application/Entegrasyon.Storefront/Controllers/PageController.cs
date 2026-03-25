using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.Storefront.Controllers;

public class PageController(
    IStorefrontTenantContext tenant,
    IStorefrontPageManager pageManager) : Controller
{
    private static readonly Dictionary<string, string> LegalSlugToProperty = new()
    {
        ["hakkimizda"] = nameof(StorefrontSettings.AboutHtml),
        ["iade-politikasi"] = nameof(StorefrontSettings.ReturnPolicyHtml),
        ["gizlilik-politikasi"] = nameof(StorefrontSettings.PrivacyPolicyHtml),
        ["kullanim-kosullari"] = nameof(StorefrontSettings.TermsHtml),
        ["kvkk"] = nameof(StorefrontSettings.KvkkHtml),
        ["cerez-politikasi"] = nameof(StorefrontSettings.CookiePolicyHtml),
        ["mesafeli-satis-sozlesmesi"] = nameof(StorefrontSettings.DistanceSalesContractHtml),
        ["on-bilgilendirme-formu"] = nameof(StorefrontSettings.PreInfoFormHtml),
        ["teslimat-kosullari"] = nameof(StorefrontSettings.DeliveryTermsHtml)
    };

    private static readonly Dictionary<string, string> LegalSlugToTitle = new()
    {
        ["hakkimizda"] = "Hakkimizda",
        ["iade-politikasi"] = "Iade Politikasi",
        ["gizlilik-politikasi"] = "Gizlilik Politikasi",
        ["kullanim-kosullari"] = "Kullanim Kosullari",
        ["kvkk"] = "KVKK Aydinlatma Metni",
        ["cerez-politikasi"] = "Cerez Politikasi",
        ["mesafeli-satis-sozlesmesi"] = "Mesafeli Satis Sozlesmesi",
        ["on-bilgilendirme-formu"] = "On Bilgilendirme Formu",
        ["teslimat-kosullari"] = "Teslimat Kosullari"
    };

    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Show(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        slug = slug.ToLowerInvariant();

        // Check if it's a legal page from settings
        if (LegalSlugToProperty.TryGetValue(slug, out var propertyName))
        {
            var property = typeof(StorefrontSettings).GetProperty(propertyName);
            var html = property?.GetValue(tenant.Settings) as string;

            if (string.IsNullOrWhiteSpace(html))
                return NotFound();

            ViewBag.Title = LegalSlugToTitle.GetValueOrDefault(slug, slug);
            ViewBag.ContentHtml = html;
            ViewBag.SeoDescription = (string?)null;
            return View();
        }

        // Check if it's a dynamic page
        var pageResult = await pageManager.GetBySlugAsync(tenant.TenantId, slug);
        if (!pageResult.Success || pageResult.Data is null)
            return NotFound();

        var page = pageResult.Data;
        ViewBag.Title = page.Title;
        ViewBag.ContentHtml = page.ContentHtml;
        ViewBag.SeoDescription = page.SeoDescription;

        return View();
    }
}
