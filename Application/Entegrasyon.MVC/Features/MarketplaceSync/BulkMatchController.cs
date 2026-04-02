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
    ICategoryService categoryService,
    IMarketPlaceManager marketPlaceManager) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/BulkMatch";

    [HttpGet("/marketplace/sync/categories/bulk")]
    public async Task<IActionResult> Index(int mp = 1)
    {
        ViewData.SetPageTitle("Toplu Kategori Esleme");
        ViewData.SetActiveNav("marketplace-sync");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyonu", "/marketplace/sync"),
            ("Kategori Eslemesi", "/marketplace/sync/categories"),
            ("Toplu Esleme", null));

        var marketPlaces = await marketPlaceManager.GetAllAsync();
        var summary = await categoryMatchService.GetCategoryMatchSummaryAsync(mp);
        var mappings = await categoryMatchService.GetAllCategoryMappingsAsync(mp);
        var allCategories = await categoryService.GetAllCategoriesWithoutAttributesAsync();

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
            UnmappedCategories = unmapped
        };

        if (Request.IsHtmx() && Request.HtmxTarget() == "unmapped-list-container")
            return PartialView($"{ViewBase}/Partials/_UnmappedList.cshtml", vm);

        return View($"{ViewBase}/Index.cshtml", vm);
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
                    msg += $" {data.FailedCount} basarisiz.";
                if (data.SkippedCount > 0)
                    msg += $" {data.SkippedCount} atlandi.";

                Response.HtmxTriggerWithData("showToast",
                    new { message = msg, type = data.FailedCount > 0 ? "warning" : "success" });
                Response.HtmxTrigger("refreshUnmapped");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Toplu esleme basarisiz.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess($"{result.Data!.SuccessCount} kategori eslendi.");
        else
            TempData.SetError(result.Message ?? "Toplu esleme basarisiz.");

        return RedirectToAction(nameof(Index), new { mp });
    }
}
