using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

[Authorize]
public class CategorySyncController(
    ICategoryMatchService categoryMatchService,
    ICategoryService categoryService,
    IMarketPlaceManager marketPlaceManager) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/CategorySync";

    [HttpGet("/marketplace/sync/categories")]
    public async Task<IActionResult> Index(int mp = 1)
    {
        ViewData.SetPageTitle("Kategori Eslemesi");
        ViewData.SetActiveNav("marketplace-sync");

        var marketPlaces = await marketPlaceManager.GetAllAsync();
        var summary = await categoryMatchService.GetCategoryMatchSummaryAsync(mp);
        var mappings = await categoryMatchService.GetAllCategoryMappingsAsync(mp);
        var allCategories = await categoryService.GetAllCategoriesWithoutAttributesAsync();

        var mappedCategoryIds = mappings.Select(m => m.ApplicationCategoryId).ToHashSet();

        var vm = new CategorySyncVm
        {
            SelectedMarketPlaceId = mp,
            MarketPlaces = marketPlaces.Data ?? [],
            Summary = summary,
            Mappings = mappings,
            AllCategories = allCategories.Select(c => new CategoryListItemVm
            {
                Id = c.Id,
                Name = c.Name ?? string.Empty,
                IsMapped = mappedCategoryIds.Contains(c.Id),
                MarketPlaceCategoryId = mappings.FirstOrDefault(m => m.ApplicationCategoryId == c.Id)?.MarketPlaceCategoryId,
                MarketPlaceCategoryName = mappings.FirstOrDefault(m => m.ApplicationCategoryId == c.Id)?.MarketPlaceCategoryName
            }).ToList()
        };

        if (Request.IsHtmx())
            return PartialView($"{ViewBase}/Partials/_CategoryMappingTable.cshtml", vm);

        return View($"{ViewBase}/Index.cshtml", vm);
    }

    [HttpPost("/marketplace/sync/categories/{categoryId:int}/map")]
    public async Task<IActionResult> CreateMapping(int categoryId, int mp, int externalCategoryId, string? externalCategoryName)
    {
        var dto = new CreateCategoryMarketplaceMatchDto
        {
            ApplicationCategoryId = categoryId,
            MarketPlaceId = mp,
            MarketPlaceCategoryId = externalCategoryId,
            MarketPlaceCategoryName = externalCategoryName
        };

        var result = await categoryMatchService.CreateCategoryMappingAsync(dto);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Kategori eslesmesi olusturuldu.", type = "success" });
                Response.HtmxTrigger("refreshTable");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eslestirme olusturulamadi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Kategori eslesmesi olusturuldu.");
        else
            TempData.SetError(result.Message ?? "Eslestirme olusturulamadi.");

        return RedirectToAction(nameof(Index), new { mp });
    }

    [HttpPost("/marketplace/sync/categories/{categoryId:int}/unmap")]
    public async Task<IActionResult> RemoveMapping(int categoryId, int mp)
    {
        var result = await categoryMatchService.RemoveCategoryMappingAsync(categoryId, mp);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Kategori eslesmesi kaldirildi.", type = "success" });
                Response.HtmxTrigger("refreshTable");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Eslestirme kaldirilamadi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Kategori eslesmesi kaldirildi.");
        else
            TempData.SetError(result.Message ?? "Eslestirme kaldirilamadi.");

        return RedirectToAction(nameof(Index), new { mp });
    }
}
