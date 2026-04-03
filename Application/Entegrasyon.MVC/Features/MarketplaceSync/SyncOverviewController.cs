using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

[Authorize]
public class SyncOverviewController(
    IMarketPlaceManager marketPlaceManager,
    IProductSyncManager productSyncManager,
    ICategoryMatchService categoryMatchService,
    IBrandMatchService brandMatchService) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/Overview";

    [HttpGet("/marketplace/sync")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Pazaryeri Senkronizasyon");
        ViewData.SetActiveNav("marketplace-sync");
        ViewData.SetBreadcrumb(("Pazaryeri Senkronizasyon", null));

        var marketPlacesResult = await marketPlaceManager.GetAllAsync();
        var marketPlaces = marketPlacesResult.Success ? marketPlacesResult.Data : [];

        var cards = new List<MarketplaceSyncCardVm>();
        foreach (var mp in marketPlaces.Where(m => !m.IsDeleted))
        {
            var syncSummary = await productSyncManager.GetSyncSummaryAsync(mp.Id);
            cards.Add(new MarketplaceSyncCardVm
            {
                MarketPlaceId = mp.Id,
                Name = mp.Name,
                IsActive = !mp.IsDeleted,
                SyncSummary = syncSummary
            });
        }

        var categorySummary = await categoryMatchService.GetCategoryMatchSummaryAsync();
        var brandSummary = await brandMatchService.GetBrandMappingsSummaryAsync();

        // Enrich cards with per-marketplace category mapped counts
        foreach (var card in cards)
        {
            var perMp = categorySummary.PerMarketplace
                .FirstOrDefault(p => p.MarketPlaceId == card.MarketPlaceId);
            card.MappedCategoryCount = perMp?.MappedCount ?? 0;
        }

        var vm = new SyncOverviewVm
        {
            MarketplaceCards = cards,
            CategorySummary = categorySummary,
            BrandSummary = brandSummary
        };

        return View($"{ViewBase}/Index.cshtml", vm);
    }
}
