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
public class AttributeController(
    ICategoryAttributeManager categoryAttributeManager,
    ICategoryAttributeValueManager categoryAttributeValueManager) : Controller
{
    private const string viewBase = "~/Features/Attributes/Views";

    [HttpGet("/attributes")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Özellikler");
        ViewData.SetActiveNav("attributes");

        var result = await categoryAttributeManager.GetCategoryAttributesPageable(
            new SearchablePageDto(search ?? "", page - 1, 50));

        ViewBag.Search = search;

        // HTMX arama/sayfalama: yalnız liste paneli yenilenir.
        if (Request.IsHtmx())
            return PartialView($"{viewBase}/Partials/_AttributeList.cshtml", result.Data);

        return View($"{viewBase}/Index.cshtml", result.Data);
    }

    private void SetCreateViewData()
    {
        ViewData.SetPageTitle("Yeni Özellik");
        ViewData.SetActiveNav("attributes");
        ViewData.SetBreadcrumb(("Özellikler", "/attributes"), ("Yeni Özellik", (string?)null));
    }

    [HttpGet("/attributes/create")]
    public IActionResult Create(string? returnUrl = null)
    {
        SetCreateViewData();

        var vm = new CreateAttributeVm
        {
            // Open-redirect koruması: yalnız local URL taşınır.
            ReturnUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null
        };
        return View($"{viewBase}/Create.cshtml", vm);
    }

    [HttpPost("/attributes/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAttribute([FromForm] CreateAttributeVm model)
    {
        var name = model.Name.Trim();
        var values = (model.Values ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => new CategoryAttributeValue { Id = 0, Name = v })
            .ToList();

        // Key = Humanized = ad; junction bayrakları (IsRequired/IsVarianter/IsSlicer) burada anlamsız → false.
        var dto = new AddCategoryAttributeDto(0, false, false, name, false, name, values);
        var result = await categoryAttributeManager.AddCategoryAttribute(dto);

        var safeReturnUrl = !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? model.ReturnUrl
            : null;

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = $"\"{name}\" özelliği oluşturuldu.", type = "success" });
                Response.Headers["HX-Redirect"] = safeReturnUrl ?? "/attributes";
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Özellik oluşturulamadı.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
        {
            TempData.SetSuccess($"\"{name}\" özelliği oluşturuldu.");
            return safeReturnUrl is not null ? Redirect(safeReturnUrl) : RedirectToAction(nameof(Index));
        }

        // Hata (örn. duplicate anahtar): girilen değerler korunarak form yeniden gösterilir.
        TempData.SetError(result.Message ?? "Özellik oluşturulamadı.");
        SetCreateViewData();
        return View($"{viewBase}/Create.cshtml", model);
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
                    new { message = "Özellik güncellendi.", type = "success" });
                // Reload the detail panel with fresh data
                return await Detail(id);
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Güncelleme başarısız.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Özellik başarıyla güncellendi.");
        else
            TempData.SetError(result.Message ?? "Özellik güncellenemedi.");

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
                    new { message = "Özellik silindi.", type = "success" });
                return Content("<div class='card'><div class='card-body text-center py-5'><h3 class='text-secondary'>Özellik silindi</h3><p class='text-secondary'>Sol listeden başka bir özellik seçin.</p></div></div>", "text/html");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Özellik başarıyla silindi.");
        else
            TempData.SetError(result.Message ?? "Özellik silinemedi.");

        return RedirectToAction(nameof(Index));
    }

    // ── Attribute Value Management ────────────────────────────────────────────

    [HttpGet("/attributes/{attributeId:int}/values")]
    public async Task<IActionResult> Values(int attributeId)
    {
        var attrResult = await categoryAttributeManager.GetCategoryAttributeById(attributeId);
        if (!attrResult.Success)
            return NotFound();

        var attr = attrResult.Data!;
        var allValues = await categoryAttributeValueManager.GetValuesByCategoryAttributeId(attributeId);
        var rows = allValues
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.Name)
            .Select(v => new AttributeValueRow(v.Id, v.Name ?? ""))
            .ToList();

        var vm = new AttributeValuesVm(
            attributeId,
            attr.CategoryAttributeHumanized ?? attr.CategoryAttributeKey ?? "",
            rows);

        if (Request.IsHtmx())
            return PartialView($"{viewBase}/Partials/_AttributeValueTable.cshtml", vm);

        ViewData.SetPageTitle($"{vm.AttributeName} — Değerler");
        ViewData.SetActiveNav("attributes");
        ViewData.SetBreadcrumb(
            ("Özellikler", "/attributes"),
            (vm.AttributeName, (string?)$"/attributes/{attributeId}/values"),
            ("Değerler", (string?)null));

        return View($"{viewBase}/Values.cshtml", vm);
    }

    [HttpPost("/attributes/{attributeId:int}/values")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValuesCreate(int attributeId, [FromForm] string name)
    {
        var trimmed = NormalizeAndValidateName(name);
        if (trimmed is null)
            return RedirectToAction(nameof(Values), new { attributeId });

        // attributeId varlık/ownership kontrolü (IDOR + temiz 404; GetOrCreate aksi halde FK hatası → 500).
        var attrResult = await categoryAttributeManager.GetCategoryAttributeById(attributeId);
        if (!attrResult.Success)
            return NotFound();

        // GetOrCreate idempotent: aynı kanonik değer zaten varsa mevcut id'yi döner (ekstra precheck gerekmez).
        await categoryAttributeValueManager.GetOrCreate(attributeId, trimmed);
        TempData.SetSuccess("Değer kaydedildi.");
        return RedirectToAction(nameof(Values), new { attributeId });
    }

    [HttpPost("/attributes/values/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValueEdit(int id, [FromForm] int attributeId, [FromForm] string name)
    {
        var trimmed = NormalizeAndValidateName(name);
        if (trimmed is null)
            return RedirectToAction(nameof(Values), new { attributeId });

        // attributeId scope = IDOR koruması (değer başka attribute'a aitse güncellenmez).
        var ok = await categoryAttributeValueManager.UpdateName(id, attributeId, trimmed);
        if (ok) TempData.SetSuccess("Değer güncellendi.");
        else TempData.SetError("Değer bulunamadı.");
        return RedirectToAction(nameof(Values), new { attributeId });
    }

    [HttpPost("/attributes/values/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValueDelete(int id, [FromForm] int attributeId)
    {
        // attributeId scope = IDOR koruması.
        var ok = await categoryAttributeValueManager.SoftDelete(id, attributeId);
        if (ok) TempData.SetSuccess("Değer silindi.");
        else TempData.SetError("Değer bulunamadı.");
        return RedirectToAction(nameof(Values), new { attributeId });
    }

    // ── Inline value-create (wizard AJAX / JSON) ──────────────────────────────

    [HttpPost("/attributes/values/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValueCreate([FromForm] int categoryAttributeId, [FromForm] string name)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0 || categoryAttributeId <= 0)
            return BadRequest("Geçersiz değer.");
        if (name.Length > CategoryAttributeValue.MaxNameLength)
            return BadRequest($"Değer adı en fazla {CategoryAttributeValue.MaxNameLength} karakter olabilir.");

        var id = await categoryAttributeValueManager.GetOrCreate(categoryAttributeId, name);
        return Json(new { id, name });
    }

    // Değer adını trim'ler + (boş/uzunluk) doğrular; geçersizse TempData hatası yazıp null döner.
    private string? NormalizeAndValidateName(string? raw)
    {
        var trimmed = raw?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            TempData.SetError("Değer adı boş olamaz.");
            return null;
        }
        if (trimmed.Length > CategoryAttributeValue.MaxNameLength)
        {
            TempData.SetError($"Değer adı en fazla {CategoryAttributeValue.MaxNameLength} karakter olabilir.");
            return null;
        }
        return trimmed;
    }
}
