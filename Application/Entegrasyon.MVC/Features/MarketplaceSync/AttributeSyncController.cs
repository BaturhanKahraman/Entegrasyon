using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

[Authorize]
public class AttributeSyncController(
    IAttributeMatchManager attributeMatchManager,
    ICategoryAttributeManager categoryAttributeManager,
    IMarketPlaceManager marketPlaceManager) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/AttributeSync";

    [HttpGet("/marketplace/sync/attributes")]
    public async Task<IActionResult> Index(int mp = 1, string? search = null, string? returnUrl = null)
    {
        ViewData.SetPageTitle("Ozellik Eslemesi");
        ViewData.SetActiveNav("marketplace-sync");

        var marketPlaces = await marketPlaceManager.GetAllAsync();
        var attributesResult = await categoryAttributeManager.GetCategoryAttributes();
        var attributes = attributesResult.Data ?? [];

        var attributeIds = attributes.Select(a => a.Id).ToList();
        var matches = await attributeMatchManager.GetAttributeMatchesAsync(mp, attributeIds);
        var matchedIds = matches.ToDictionary(m => m.ApplicationCategoryAttributeId);

        var vm = new AttributeSyncVm
        {
            SelectedMarketPlaceId = mp,
            MarketPlaces = marketPlaces.Data ?? [],
            SearchTerm = search,
            ReturnUrl = returnUrl,
            Attributes = attributes.Select(a => new AttributeListItemVm
            {
                Id = a.Id,
                Key = a.CategoryAttributeKey ?? string.Empty,
                Humanized = a.CategoryAttributeHumanized,
                IsMapped = matchedIds.ContainsKey(a.Id),
                MarketPlaceAttributeId = matchedIds.TryGetValue(a.Id, out var m) ? m.MarketPlaceCategoryAttributeId : null
            }).ToList()
        };

        if (Request.IsHtmx() && Request.HtmxTarget() == "attribute-list-container")
            return PartialView($"{ViewBase}/Partials/_AttributeList.cshtml", vm);

        return View($"{ViewBase}/Index.cshtml", vm);
    }

    [HttpGet("/marketplace/sync/attributes/{attributeId:int}/detail")]
    public async Task<IActionResult> Detail(int attributeId, int mp = 1, string? returnUrl = null)
    {
        var attrResult = await categoryAttributeManager.GetCategoryAttributeById(attributeId);
        if (!attrResult.Success || attrResult.Data is null)
            return NotFound();

        var attr = attrResult.Data;
        var matches = await attributeMatchManager.GetAttributeMatchesAsync(mp, [attributeId]);
        var attrMatch = matches.FirstOrDefault();

        var valueIds = attr.CategoryAttributeValues.Select(v => v.Id).ToList();
        var valueMatches = valueIds.Count > 0
            ? await attributeMatchManager.GetValueMatchesAsync(mp, valueIds)
            : [];
        var valueMatchDict = valueMatches.ToDictionary(vm => vm.ApplicationCategoryAttributeValueId);

        var vm = new AttributeMatchPanelVm
        {
            AttributeId = attr.Id,
            AttributeKey = attr.CategoryAttributeKey ?? string.Empty,
            AttributeHumanized = attr.CategoryAttributeHumanized,
            MarketPlaceId = mp,
            AttributeMatch = attrMatch,
            ReturnUrl = returnUrl,
            ValueMatches = attr.CategoryAttributeValues.Select(v => new AttributeValueMatchItemVm
            {
                ValueId = v.Id,
                ValueName = v.Name ?? string.Empty,
                IsMapped = valueMatchDict.ContainsKey(v.Id),
                MarketPlaceValueId = valueMatchDict.TryGetValue(v.Id, out var vm) ? vm.MarketPlaceCategoryAttributeValueId : null,
                MarketPlaceValueExternalId = valueMatchDict.TryGetValue(v.Id, out var vm2) ? vm2.MarketPlaceCategoryAttributeValueExternalId : null
            }).ToList()
        };

        return PartialView($"{ViewBase}/Partials/_AttributeMatchPanel.cshtml", vm);
    }

    [HttpPost("/marketplace/sync/attributes/{attributeId:int}/map")]
    public async Task<IActionResult> SaveAttributeMatch(int attributeId, int mp, int mpAttributeId, string? externalId, string? returnUrl)
    {
        var result = await attributeMatchManager.SaveAttributeMatchAsync(attributeId, mp, mpAttributeId, externalId);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    Response.HtmxTriggerWithData("showToast",
                        new { message = "Ozellik eslesmesi kaydedildi. Yonlendiriliyorsunuz...", type = "success" });
                    Response.Headers["HX-Redirect"] = returnUrl;
                    return Content("");
                }

                Response.HtmxTriggerWithData("showToast",
                    new { message = "Ozellik eslesmesi kaydedildi.", type = "success" });
                Response.HtmxTrigger("refreshList");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eslestirme kaydedilemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success && !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index), new { mp });
    }

    [HttpPost("/marketplace/sync/attributes/{attributeId:int}/unmap")]
    public async Task<IActionResult> RemoveAttributeMatch(int attributeId, int mp)
    {
        var result = await attributeMatchManager.RemoveAttributeMatchAsync(attributeId, mp);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Ozellik eslesmesi kaldirildi.", type = "success" });
                Response.HtmxTrigger("refreshList");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eslestirme kaldirilamadi.", type = "danger" });
            return StatusCode(422);
        }

        return RedirectToAction(nameof(Index), new { mp });
    }

    [HttpPost("/marketplace/sync/attributes/values/{valueId:int}/map")]
    public async Task<IActionResult> SaveValueMatch(int valueId, int mp, int mpValueId, string? externalId)
    {
        var result = await attributeMatchManager.SaveValueMatchAsync(valueId, mp, mpValueId, externalId);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Deger eslesmesi kaydedildi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eslestirme kaydedilemedi.", type = "danger" });
            return StatusCode(422);
        }

        return RedirectToAction(nameof(Index), new { mp });
    }

    [HttpPost("/marketplace/sync/attributes/values/{valueId:int}/unmap")]
    public async Task<IActionResult> RemoveValueMatch(int valueId, int mp)
    {
        var result = await attributeMatchManager.RemoveValueMatchAsync(valueId, mp);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Deger eslesmesi kaldirildi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eslestirme kaldirilamadi.", type = "danger" });
            return StatusCode(422);
        }

        return RedirectToAction(nameof(Index), new { mp });
    }
}
