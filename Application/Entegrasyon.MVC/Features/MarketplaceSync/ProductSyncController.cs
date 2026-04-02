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
    IProductSyncManager productSyncManager) : Controller
{
    private const string ViewBase = "~/Features/MarketplaceSync/Views/ProductSync";

    [HttpGet("/marketplace/matching")]
    public async Task<IActionResult> Index(
        int mp = 1,
        string? state = null,
        string? search = null,
        int page = 1)
    {
        ViewData.SetPageTitle("Urun Senkronizasyon");
        ViewData.SetActiveNav("marketplace-sync");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyon", "/marketplace/sync"),
            ("Urun Senkronizasyon", null));

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
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await productSyncManager.GetProductSyncDetailAsync(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Urun bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle(result.Data!.Title);
        ViewData.SetActiveNav("marketplace-sync");
        ViewData.SetBreadcrumb(
            ("Pazaryeri Senkronizasyon", "/marketplace/sync"),
            ("Urun Senkronizasyon", "/marketplace/matching"),
            (result.Data.Title, null));

        return View($"{ViewBase}/Detail.cshtml", result.Data);
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
                    new { message = result.Message ?? "Senkronizasyon basarisiz.", type = "danger" });
            }

            return Content("");
        }

        if (result.Success)
            TempData.SetSuccess("Senkronizasyon baslatildi.");
        else
            TempData.SetError(result.Message ?? "Senkronizasyon basarisiz.");

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
                    new { message = result.Message ?? "Toplu senkronizasyon basarisiz.", type = "danger" });
            }

            return Content("");
        }

        if (result.Success)
            TempData.SetSuccess("Toplu senkronizasyon baslatildi.");
        else
            TempData.SetError(result.Message ?? "Toplu senkronizasyon basarisiz.");

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
                    new { message = "Hatali urunler yeniden deneniyor.", type = "success" });
                Response.HtmxTrigger("refreshTable");
            }
            else
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = result.Message ?? "Yeniden deneme basarisiz.", type = "danger" });
            }

            return Content("");
        }

        if (result.Success)
            TempData.SetSuccess("Hatali urunler yeniden deneniyor.");
        else
            TempData.SetError(result.Message ?? "Yeniden deneme basarisiz.");

        return RedirectToAction(nameof(Index), new { mp });
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
