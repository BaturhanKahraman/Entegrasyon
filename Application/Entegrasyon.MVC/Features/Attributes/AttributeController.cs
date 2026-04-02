using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Attributes;

[Authorize]
public class AttributeController(ICategoryAttributeManager categoryAttributeManager) : Controller
{
    private const string ViewBase = "~/Features/Attributes/Views";

    [HttpGet("/attributes")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Ozellikler");
        ViewData.SetActiveNav("attributes");

        var result = await categoryAttributeManager.GetCategoryAttributesPageable(
            new SearchablePageDto(search ?? "", page - 1, 50));

        ViewBag.Search = search;
        return View($"{ViewBase}/Index.cshtml", result.Data);
    }

    [HttpGet("/attributes/{id:int}/detail")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await categoryAttributeManager.GetCategoryAttributeById(id);
        if (!result.Success)
            return NotFound();

        return PartialView($"{ViewBase}/Partials/_AttributeDetail.cshtml", result.Data);
    }
}
