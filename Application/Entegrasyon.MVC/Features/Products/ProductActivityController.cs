using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.MVC.Features.Products.ViewModels.Activity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Products;

/// <summary>
/// Ürün 360° aktivite detay sayfasının HTMX lazy-load endpoint'leri.
/// Her sekme (Aktivite / Siparişler / Stok Hareketleri) ve pazaryeri durum kartları ayrı partial döner.
///
/// E-ticaret/pazaryeri OPSİYONEL: <see cref="IProductActivityPageManager"/> feature kapalıyken
/// pazaryeri/aktivite sorgularını çalıştırmaz; controller bunu VM'deki EcommerceEnabled=false ile iletir.
/// Sipariş/stok sekmeleri fiziksel mağaza için de çalışır.
/// </summary>
[Authorize]
public sealed class ProductActivityController(IProductActivityPageManager pageManager) : Controller
{
    [HttpGet("/products/{id:guid}/activity")]
    public async Task<IActionResult> Activity(
        Guid id,
        [FromQuery(Name = "marketplace")] string[]? marketplaces = null,
        [FromQuery(Name = "activityType")] string[]? activityTypes = null,
        [FromQuery(Name = "status")] string? status = null,
        [FromQuery(Name = "from")] DateTimeOffset? from = null,
        [FromQuery(Name = "to")] DateTimeOffset? to = null,
        [FromQuery(Name = "cursor")] DateTimeOffset? cursor = null,
        [FromQuery(Name = "cursorId")] long? cursorId = null,
        [FromQuery(Name = "pageSize")] int pageSize = 20)
    {
        if (pageSize is <= 0 or > 100) pageSize = 20;

        var filter = new ProductActivityTimelineFilter(
            MarketplaceNames: NormalizeStrings(marketplaces),
            ActivityTypes: ParseEnums<ProductActivityType>(activityTypes),
            Statuses: ParseEnums<ProductActivityStatus>(status is null ? null : [status]),
            From: from,
            To: to,
            Cursor: cursor,
            CursorId: cursorId);

        // M3: feature'ı istek başına TEK kez çöz, manager'a geçir (manager içeride tekrar sormaz).
        var enabled = await pageManager.IsEcommerceEnabledAsync();
        var result = await pageManager.GetTimelineAsync(id, filter, pageSize, enabled);
        var items = result.Data ?? [];
        var hasMore = items.Count == pageSize;

        var vm = new ProductActivityTimelineVm
        {
            ProductId = id,
            EcommerceEnabled = enabled,
            Items = items,
            PageSize = pageSize,
            HasMore = hasMore,
            NextCursor = hasMore ? items[^1].CreatedAt : null,
            NextCursorId = hasMore ? items[^1].Id : null,
            AppliedFilter = filter
        };

        return PartialView("Partials/Activity/_ActivityTimeline", vm);
    }

    [HttpGet("/products/{id:guid}/marketplace-cards")]
    public async Task<IActionResult> MarketplaceCards(Guid id)
    {
        var enabled = await pageManager.IsEcommerceEnabledAsync();
        var result = await pageManager.GetMarketplaceStatusesAsync(id, enabled);

        var vm = new ProductMarketplaceCardsVm
        {
            ProductId = id,
            EcommerceEnabled = enabled,
            Cards = result.Data ?? []
        };

        return PartialView("Partials/Activity/_ProductMarketplaceCards", vm);
    }

    [HttpGet("/products/{id:guid}/orders")]
    public async Task<IActionResult> Orders(Guid id)
    {
        var result = await pageManager.GetOrdersAsync(id);
        return PartialView("Partials/Activity/_ProductOrders", result.Data ?? []);
    }

    [HttpGet("/products/{id:guid}/stock-movements")]
    public async Task<IActionResult> StockMovements(Guid id)
    {
        var result = await pageManager.GetStockMovementsAsync(id);
        return PartialView("Partials/Activity/_ProductStockMovements", result.Data ?? []);
    }

    private static IReadOnlyList<string>? NormalizeStrings(string[]? values)
    {
        if (values is null) return null;
        var cleaned = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct()
            .ToList();
        return cleaned.Count > 0 ? cleaned : null;
    }

    private static IReadOnlyList<TEnum>? ParseEnums<TEnum>(string[]? values) where TEnum : struct, Enum
    {
        if (values is null) return null;
        var parsed = new List<TEnum>();
        foreach (var v in values)
        {
            if (Enum.TryParse<TEnum>(v?.Trim(), ignoreCase: true, out var e) && Enum.IsDefined(e))
                parsed.Add(e);
        }
        return parsed.Count > 0 ? parsed : null;
    }
}
