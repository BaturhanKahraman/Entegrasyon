using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.Features.Attributes.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Attributes;

[Authorize]
public class AttributeController(ICategoryAttributeManager categoryAttributeManager) : Controller
{
    private const string viewBase = "~/Features/Attributes/Views";

    [HttpGet("/attributes")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Ozellikler");
        ViewData.SetActiveNav("attributes");

        var result = await categoryAttributeManager.GetCategoryAttributesPageable(
            new SearchablePageDto(search ?? "", page - 1, 50));

        ViewBag.Search = search;
        return View($"{viewBase}/Index.cshtml", result.Data);
    }

    [HttpGet("/attributes/{id:int}/detail")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await categoryAttributeManager.GetCategoryAttributeById(id);
        if (!result.Success)
            return NotFound();

        return PartialView($"{viewBase}/Partials/_AttributeDetail.cshtml", result.Data);
    }

    [HttpGet("/attributes/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await categoryAttributeManager.GetCategoryAttributeById(id);
        if (!result.Success)
            return NotFound();

        var attr = result.Data!;
        var vm = new EditAttributeVm
        {
            Id = attr.Id,
            CategoryAttributeKey = attr.CategoryAttributeKey ?? "",
            CategoryAttributeHumanized = attr.CategoryAttributeHumanized ?? "",
            ExistingValues = attr.CategoryAttributeValues
                .OrderBy(v => v.Name)
                .Select(v => new AttributeValueVm { Id = v.Id, Name = v.Name ?? "" })
                .ToList()
        };

        return PartialView($"{viewBase}/Partials/_AttributeEditForm.cshtml", vm);
    }

    [HttpPost("/attributes/{id:int}/update")]
    public async Task<IActionResult> Update(int id, [FromForm] EditAttributeVm model)
    {
        // Build value list: keep non-removed existing values + add new ones
        var values = model.ExistingValues
            .Where(v => !v.Remove)
            .Select(v => new CategoryAttributeValue { Id = v.Id, Name = v.Name, CategoryAttributeId = id })
            .ToList();

        // Parse new values (comma-separated)
        if (!string.IsNullOrWhiteSpace(model.NewValues))
        {
            var newNames = model.NewValues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var name in newNames)
            {
                values.Add(new CategoryAttributeValue { Id = 0, Name = name, CategoryAttributeId = id });
            }
        }

        var dto = new EditCategoryAttributeDto(
            Id: id,
            IsRequired: false,
            IsVarianter: false,
            CategoryAttributeKey: model.CategoryAttributeKey,
            IsSlicer: false,
            CategoryAttributeHumanized: model.CategoryAttributeHumanized,
            CategoryAttributeValues: values,
            CategoryId: 0
        );

        var result = await categoryAttributeManager.UpdateCategoryAttribute(dto);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Ozellik guncellendi.", type = "success" });
                // Reload the detail panel with fresh data
                return await Detail(id);
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Guncelleme başarısız.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Ozellik basariyla guncellendi.");
        else
            TempData.SetError(result.Message ?? "Ozellik guncellenemedi.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/attributes/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await categoryAttributeManager.DeleteCategoryAttribute(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Ozellik silindi.", type = "success" });
                return Content("<div class='card'><div class='card-body text-center py-5'><h3 class='text-secondary'>Ozellik silindi</h3><p class='text-secondary'>Sol listeden baska bir ozellik secin.</p></div></div>", "text/html");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Ozellik basariyla silindi.");
        else
            TempData.SetError(result.Message ?? "Ozellik silinemedi.");

        return RedirectToAction(nameof(Index));
    }
}
