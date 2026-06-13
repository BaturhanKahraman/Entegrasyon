using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

/// <summary>
/// Kategori eşleme wizard sayfası — eski modal akışının yerini alır (modal kapanmama bug'ı çözülür).
/// Tek kategori odaklı: pazaryeri kategorisi seç → zorunlu özellik tamlığını göster → değerler.
/// </summary>
[Authorize]
public class CategoryMappingWizardController(
    ICategoryService categoryService,
    ICategoryMatchService categoryMatchService,
    ICategoryMatchValidationService validationService,
    IMarketPlaceManager marketPlaceManager) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/CategoryMappingWizard";

    [HttpGet("/marketplace/sync/categories/{categoryId:int}/wizard")]
    public async Task<IActionResult> Index(int categoryId, int mp = 1)
    {
        var category = await categoryService.GetCategoryById(categoryId);
        if (category is null)
            return NotFound();

        var marketPlacesResult = await marketPlaceManager.GetAllAsync();
        var marketPlaces = marketPlacesResult.Data ?? [];
        var marketPlaceName = marketPlaces.FirstOrDefault(x => x.Id == mp)?.Name ?? $"Pazaryeri #{mp}";

        var mappings = await categoryMatchService.GetAllCategoryMappingsAsync(mp);
        var mapping = mappings.FirstOrDefault(m => m.ApplicationCategoryId == categoryId);
        var isMapped = mapping is not null;

        Entegrasyon.Entity.Dtos.Category.CategoryMatchValidationResultDto? validation = null;
        var sentProductCount = 0;
        if (isMapped)
        {
            var validationResult = await validationService.ValidateCategoryMatchAsync(categoryId, mp);
            if (validationResult.Success)
                validation = validationResult.Data;

            sentProductCount = await categoryMatchService.GetPublishedProductCountAsync(categoryId, mp);
        }

        var vm = new CategoryMappingWizardVm
        {
            CategoryId = categoryId,
            CategoryName = category.Name ?? $"Kategori #{categoryId}",
            MarketPlaceId = mp,
            MarketPlaceName = marketPlaceName,
            MarketPlaces = marketPlaces,
            IsMapped = isMapped,
            MarketPlaceCategoryId = mapping?.MarketPlaceCategoryId,
            MarketPlaceCategoryName = mapping?.MarketPlaceCategoryName,
            Validation = validation,
            SentProductCount = sentProductCount
        };

        ViewData.SetPageTitle($"Eşleme: {vm.CategoryName}");
        ViewData.SetActiveNav("marketplace-sync");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyonu", "/marketplace/sync"),
            ("Kategori Eşlemesi", "/marketplace/sync/categories"),
            (vm.CategoryName, null));

        return View($"{ViewBase}/Index.cshtml", vm);
    }
}
