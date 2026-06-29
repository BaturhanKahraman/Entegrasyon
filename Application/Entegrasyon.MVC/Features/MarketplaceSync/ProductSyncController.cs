using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MarketplaceSync;

[Authorize]
public class ProductSyncController(
    IMarketPlaceManager marketPlaceManager,
    IProductSyncManager productSyncManager,
    IProductActivityLogger productActivityLogger) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/ProductSync";

    [HttpGet("/marketplace/matching")]
    public async Task<IActionResult> Index(
        int mp = 1,
        string? state = null,
        string? search = null,
        int page = 1)
    {
        ViewData.SetPageTitle("Ürün Senkronizasyon");
        ViewData.SetActiveNav("marketplace-matching");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyon", "/marketplace/sync"),
            ("Ürün Senkronizasyon", null));

        var stateFilter = ParseSyncState(state);
        var pageIndex = Math.Max(0, page - 1);
        const int pageSize = 20;

        var marketPlacesResult = await marketPlaceManager.GetAllAsync();
        var marketPlaces = marketPlacesResult.Success ? marketPlacesResult.Data : [];

        var summary = await productSyncManager.GetSyncSummaryAsync(mp);
        var productsResult = await productSyncManager.GetProductSyncListAsync(
            mp, stateFilter, search ?? "", pageIndex, pageSize);

        var products = productsResult.Success
            ? productsResult.Data
            : new Entity.Pageable<ProductSyncListItemDto>([], 0, pageSize, 0);

        if (Request.IsHtmx())
        {
            var tableVm = new ProductSyncTableVm
            {
                SelectedMarketPlaceId = mp,
                Products = products,
                SearchKey = search,
                StateFilter = state
            };
            return PartialView($"{ViewBase}/Partials/_SyncTable.cshtml", tableVm);
        }

        var vm = new ProductSyncIndexVm
        {
            SelectedMarketPlaceId = mp,
            MarketPlaces = marketPlaces,
            Summary = summary,
            Products = products,
            SearchKey = search,
            StateFilter = state
        };

        return View($"{ViewBase}/Index.cshtml", vm);
    }

    [HttpGet("/marketplace/matching/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, int? tab = null)
    {
        var result = await productSyncManager.GetProductSyncDetailAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Urun bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var timelineResult = await productActivityLogger.GetTimelineAsync(id, 50);

        ViewData.SetPageTitle(result.Data!.Title);
        ViewData.SetActiveNav("marketplace-matching");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyon", "/marketplace/sync"),
            ("Ürün Senkronizasyon", "/marketplace/matching"),
            (result.Data.Title, null));

        var vm = new ProductSyncDetailVm
        {
            Product = result.Data,
            ActivityTimeline = timelineResult.Success ? timelineResult.Data ?? [] : [],
            SelectedMarketPlaceTab = tab
        };

        return View($"{ViewBase}/Detail.cshtml", vm);
    }

    [HttpPost("/marketplace/matching/{id:guid}/sync")]
    public async Task<IActionResult> SyncProduct(Guid id, int mp = 1)
    {
        var result = await productSyncManager.SyncProductAsync(id, mp);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Senkronizasyon baslatildi.", type = "success" });
                Response.HtmxTrigger("refreshTable");
            }
            else
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = result.Message ?? "Senkronizasyon başarısız.", type = "danger" });
            }

            return Content("");
        }

        if (result.Success)
            TempData.SetSuccess("Senkronizasyon baslatildi.");
        else
            TempData.SetError(result.Message ?? "Senkronizasyon başarısız.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/marketplace/matching/sync-all")]
    public async Task<IActionResult> SyncAll(int mp = 1)
    {
        var result = await productSyncManager.SyncAllPendingAsync(mp);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Tum bekleyen urunler senkronize ediliyor.", type = "success" });
                Response.HtmxTrigger("refreshTable");
            }
            else
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = result.Message ?? "Toplu senkronizasyon başarısız.", type = "danger" });
            }

            return Content("");
        }

        if (result.Success)
            TempData.SetSuccess("Toplu senkronizasyon baslatildi.");
        else
            TempData.SetError(result.Message ?? "Toplu senkronizasyon başarısız.");

        return RedirectToAction(nameof(Index), new { mp });
    }

    [HttpPost("/marketplace/matching/retry-all")]
    public async Task<IActionResult> RetryAll(int mp = 1)
    {
        var result = await productSyncManager.RetryAllFailedAsync(mp);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Hatalı urunler yeniden deneniyor.", type = "success" });
                Response.HtmxTrigger("refreshTable");
            }
            else
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = result.Message ?? "Yeniden deneme başarısız.", type = "danger" });
            }

            return Content("");
        }

        if (result.Success)
            TempData.SetSuccess("Hatalı urunler yeniden deneniyor.");
        else
            TempData.SetError(result.Message ?? "Yeniden deneme başarısız.");

        return RedirectToAction(nameof(Index), new { mp });
    }

    [HttpGet("/marketplace/sync/products")]
    public async Task<IActionResult> BulkSync(int mp = 1)
    {
        ViewData.SetPageTitle("Toplu Ürün Senkronizasyon");
        ViewData.SetActiveNav("marketplace-sync");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyon", "/marketplace/sync"),
            ("Toplu Ürün Senkronizasyon", null));

        var marketPlacesResult = await marketPlaceManager.GetAllAsync();
        var marketPlaces = marketPlacesResult.Success ? marketPlacesResult.Data : [];

        var summary = await productSyncManager.GetSyncSummaryAsync(mp);

        ViewBag.SelectedMarketPlaceId = mp;
        ViewBag.MarketPlaces = marketPlaces;
        ViewBag.Summary = summary;

        return View($"{ViewBase}/BulkSync.cshtml");
    }

    private static MarketplaceSyncState? ParseSyncState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;
        return Enum.TryParse<MarketplaceSyncState>(state, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
