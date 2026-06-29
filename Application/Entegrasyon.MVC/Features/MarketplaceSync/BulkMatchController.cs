using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

[Authorize]
public class BulkMatchController(
    ICategoryMatchService categoryMatchService,
    ICategoryAutoMatchService categoryAutoMatchService,
    ICategoryService categoryService,
    IMarketPlaceManager marketPlaceManager) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/BulkMatch";

    [HttpGet("/marketplace/sync/categories/bulk")]
    public async Task<IActionResult> Index(int mp = 1)
    {
        ViewData.SetPageTitle("Toplu Kategori Eşleme");
        ViewData.SetActiveNav("marketplace-sync");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyonu", "/marketplace/sync"),
            ("Kategori Eşlemesi", "/marketplace/sync/categories"),
            ("Toplu Esleme", null));

        var marketPlaces = await marketPlaceManager.GetAllAsync();
        var summary = await categoryMatchService.GetCategoryMatchSummaryAsync(mp);
        var mappings = await categoryMatchService.GetAllCategoryMappingsAsync(mp);
        var allCategories = await categoryService.GetAllCategoriesWithoutAttributesAsync();
        var autoMatchAvailable = await categoryAutoMatchService.IsAvailableAsync();

        var mappedCategoryIds = mappings.Select(m => m.ApplicationCategoryId).ToHashSet();

        var unmapped = allCategories
            .Where(c => !mappedCategoryIds.Contains(c.Id))
            .Select(c => new UnmappedCategoryItemVm
            {
                Id = c.Id,
                Name = c.Name ?? string.Empty
            })
            .OrderBy(c => c.Name)
            .ToList();

        var vm = new BulkCategoryMatchVm
        {
            SelectedMarketPlaceId = mp,
            MarketPlaces = marketPlaces.Data ?? [],
            Summary = summary,
            UnmappedCategories = unmapped,
            AutoMatchAvailable = autoMatchAvailable
        };

        if (Request.IsHtmx() && Request.HtmxTarget() == "unmapped-list-container")
            return PartialView($"{ViewBase}/Partials/_UnmappedList.cshtml", vm);

        return View($"{ViewBase}/Index.cshtml", vm);
    }

    [HttpPost("/marketplace/sync/categories/bulk/auto-match")]
    public async Task<IActionResult> AutoMatch(int mp, int[] categoryIds, string[] categoryNames)
    {
        var items = new List<CategoryAutoMatchItemDto>();
        for (var i = 0; i < categoryIds.Length; i++)
        {
            items.Add(new CategoryAutoMatchItemDto
            {
                CategoryId = categoryIds[i],
                CategoryName = categoryNames.Length > i ? categoryNames[i] : string.Empty
            });
        }

        if (items.Count == 0)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Otomatik esleme icin kategori secilmedi.", type = "warning" });
            return StatusCode(422);
        }

        var request = new CategoryAutoMatchRequestDto
        {
            MarketPlaceId = mp,
            Categories = items
        };

        var result = await categoryAutoMatchService.GetAutoMatchSuggestionsAsync(request);

        if (!result.Success || result.Data.Count == 0)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Otomatik esleme onerisi bulunamadı.", type = "info" });
            return PartialView($"{ViewBase}/Partials/_SuggestionsEmpty.cshtml");
        }

        var vm = new BulkCategoryMatchVm
        {
            SelectedMarketPlaceId = mp,
            Suggestions = result.Data,
            SuggestionsLoaded = true
        };

        return PartialView($"{ViewBase}/Partials/_SuggestionsPanel.cshtml", vm);
    }

    [HttpPost("/marketplace/sync/categories/bulk/apply-suggestions")]
    public async Task<IActionResult> ApplySuggestions(int mp, int[] applicationCategoryIds,
        int[] suggestedMarketPlaceCategoryIds, string?[] suggestedMarketPlaceCategoryNames)
    {
        var items = new List<BulkCategoryMatchItemDto>();
        for (var i = 0; i < applicationCategoryIds.Length; i++)
        {
            if (suggestedMarketPlaceCategoryIds.Length <= i || suggestedMarketPlaceCategoryIds[i] == 0)
                continue;

            items.Add(new BulkCategoryMatchItemDto
            {
                ApplicationCategoryId = applicationCategoryIds[i],
                MarketPlaceCategoryId = suggestedMarketPlaceCategoryIds[i],
                MarketPlaceCategoryName = suggestedMarketPlaceCategoryNames?.Length > i
                    ? suggestedMarketPlaceCategoryNames[i]
                    : null
            });
        }

        if (items.Count == 0)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = "Uygulanacak oneri secilmedi.", type = "warning" });
            return StatusCode(422);
        }

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = mp,
            Items = items
        };

        var result = await categoryMatchService.BulkCreateCategoryMappingsAsync(dto);

        if (result.Success)
        {
            var data = result.Data!;
            var msg = $"{data.SuccessCount} kategori otomatik eslendi.";
            if (data.FailedCount > 0)
                msg += $" {data.FailedCount} başarısız.";
            if (data.SkippedCount > 0)
                msg += $" {data.SkippedCount} atlandi.";

            Response.HtmxTriggerWithData("showToast",
                new { message = msg, type = data.FailedCount > 0 ? "warning" : "success" });
            Response.HtmxTrigger("refreshUnmapped");
            return Content("");
        }

        Response.HtmxTriggerWithData("showToast",
            new { message = result.Message ?? "Otomatik esleme başarısız.", type = "danger" });
        return StatusCode(422);
    }

    [HttpPost("/marketplace/sync/categories/bulk/submit")]
    public async Task<IActionResult> Submit(int mp, int[] categoryIds, int[] marketPlaceCategoryIds, string?[] externalCategoryIds, string?[] marketPlaceCategoryNames)
    {
        var items = new List<BulkCategoryMatchItemDto>();
        for (var i = 0; i < categoryIds.Length; i++)
        {
            if (marketPlaceCategoryIds.Length <= i || marketPlaceCategoryIds[i] == 0)
                continue;

            items.Add(new BulkCategoryMatchItemDto
            {
                ApplicationCategoryId = categoryIds[i],
                MarketPlaceCategoryId = marketPlaceCategoryIds[i],
                ExternalCategoryId = externalCategoryIds.Length > i ? externalCategoryIds[i] : null,
                MarketPlaceCategoryName = marketPlaceCategoryNames.Length > i ? marketPlaceCategoryNames[i] : null
            });
        }

        if (items.Count == 0)
        {
            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Esleme yapilacak kategori secilmedi.", type = "warning" });
                return StatusCode(422);
            }

            TempData.SetError("Esleme yapilacak kategori secilmedi.");
            return RedirectToAction(nameof(Index), new { mp });
        }

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = mp,
            Items = items
        };

        var result = await categoryMatchService.BulkCreateCategoryMappingsAsync(dto);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                var data = result.Data!;
                var msg = $"{data.SuccessCount} kategori eslendi.";
                if (data.FailedCount > 0)
                    msg += $" {data.FailedCount} başarısız.";
                if (data.SkippedCount > 0)
                    msg += $" {data.SkippedCount} atlandi.";

                Response.HtmxTriggerWithData("showToast",
                    new { message = msg, type = data.FailedCount > 0 ? "warning" : "success" });
                Response.HtmxTrigger("refreshUnmapped");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Toplu esleme başarısız.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess($"{result.Data!.SuccessCount} kategori eslendi.");
        else
            TempData.SetError(result.Message ?? "Toplu esleme başarısız.");

        return RedirectToAction(nameof(Index), new { mp });
    }
}
