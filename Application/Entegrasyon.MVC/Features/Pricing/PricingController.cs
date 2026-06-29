using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Pricing;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Pricing;

[Authorize]
public class PricingController(
    IProductService productService,
    IProductVariantManager variantManager,
    IPricingRuleManager pricingRuleManager) : HtmxController
{
    [HttpGet("/pricing")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Fiyat Yönetimi");
        ViewData.SetActiveNav("pricing");
        ViewData.SetBreadcrumb(("Fiyat Yönetimi", null));

        var result = await productService.GetProductsDetailsPageable(
            new SearchablePageDto(search ?? "", page - 1, 30));

        ViewBag.Search = search;
        return HtmxView(result.Data);
    }

    [HttpGet("/pricing/rules")]
    public async Task<IActionResult> Rules()
    {
        ViewData.SetPageTitle("Fiyatlandirma Kurallari");
        ViewData.SetActiveNav("pricing-rules");
        ViewData.SetBreadcrumb(("Fiyat Yönetimi", "/pricing"), ("Kurallar", null));

        var rules = await pricingRuleManager.GetAllRulesAsync();
        return View(rules);
    }

    [HttpPost("/pricing/rules/create")]
    public async Task<IActionResult> CreateRule([FromForm] CreatePricingRuleDto dto)
    {
        var result = await pricingRuleManager.CreateRuleAsync(dto);
        return HtmxMutationResult(result, "Kural olusturuldu.", refreshEvent: "ruleChanged");
    }

    [HttpPost("/pricing/rules/{id:int}/toggle")]
    public async Task<IActionResult> ToggleRule(int id)
    {
        var result = await pricingRuleManager.ToggleRuleAsync(id);
        return HtmxMutationResult(result, result.Message ?? "Kural guncellendi.", refreshEvent: "ruleChanged");
    }

    [HttpPost("/pricing/rules/{id:int}/delete")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var result = await pricingRuleManager.DeleteRuleAsync(id);
        return HtmxMutationResult(result, "Kural silindi.", refreshEvent: "ruleChanged");
    }

    [HttpGet("/pricing/rules/{id:int}/preview")]
    public async Task<IActionResult> PreviewRule(int id)
    {
        var impacts = await pricingRuleManager.PreviewRuleImpactAsync(id);
        return PartialView("Partials/_RulePreview", impacts);
    }

    [HttpGet("/pricing/{productId:guid}/variants")]
    public async Task<IActionResult> ProductVariants(Guid productId)
    {
        var product = await productService.GetProductDetailById(productId);
        if (!product.Success)
            return Content("<tr><td colspan='8' class='text-danger p-2'>Urun bulunamadı.</td></tr>", "text/html");

        return PartialView("Partials/_PriceVariants", product.Data);
    }

    [HttpPost("/pricing/variant/{variantId:guid}/update-price")]
    public async Task<IActionResult> UpdateVariantPrice(Guid variantId,
        [FromForm] decimal listPrice, [FromForm] decimal salePrice)
    {
        var variant = await variantManager.GetById(variantId);
        if (variant is null)
        {
            Response.HtmxTriggerWithData("showToast", new { message = "Varyant bulunamadı.", type = "danger" });
            return StatusCode(404);
        }

        var dto = new EditProductVariantDto(
            variantId, listPrice, salePrice,
            variant.CostPrice, variant.ECommercePrice,
            variant.DimensionalWeight, variant.VatRate, variant.CurrencyType);

        var result = await variantManager.UpdateVariant(dto);
        return HtmxMutationResult(result, "Fiyat guncellendi.", refreshEvent: "priceUpdated");
    }
}
