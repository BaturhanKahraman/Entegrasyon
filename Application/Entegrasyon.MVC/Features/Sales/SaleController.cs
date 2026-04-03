using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Sales;

[Authorize]
public class SaleController(ISaleManager saleManager) : Controller
{
    [HttpGet("/sales")]
    public async Task<IActionResult> Index(
        string? search = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        int page = 1)
    {
        ViewData.SetPageTitle("Satislar");
        ViewData.SetActiveNav("sales");

        var dto = new SalePageableDto(
            CustomerId: null,
            DateBetweenStart: startDate,
            DateBetweenEnd: endDate,
            SalePersonId: Guid.Empty,
            FullTextSearchKey: search ?? "",
            PageIndex: page - 1,
            PageSize: 20);

        var result = await saleManager.GetSalesPageable(dto);

        if (Request.IsHtmx())
            return PartialView("Partials/_SaleTable", result.Data);

        ViewBag.Search = search;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        return View(result.Data);
    }
}
