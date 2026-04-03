using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Templates;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Templates;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MatchedEntityImport;

[Authorize]
public class MatchedEntityImportController(IMatchedEntityImportManager matchedEntityImportManager) : HtmxController
{
    private const string ViewBase = "~/Features/MatchedEntityImport/Views";

    [HttpGet("/matched-entities")]
    public async Task<IActionResult> Index(
        string? search = null, int? type = null, int page = 1, CancellationToken ct = default)
    {
        ViewData.SetPageTitle("Eslestirilmis Varlik Aktarimi");
        ViewData.SetActiveNav("matched-entities");

        var typeFilter = type.HasValue ? (MatchedEntityType)type.Value : (MatchedEntityType?)null;
        var result = await matchedEntityImportManager.GetAvailablePackagesAsync(
            new MatchedEntityPackagePaginatedRequest
            {
                SearchTerm = search,
                EntityType = typeFilter,
                PageIndex = page - 1,
                PageSize = 20
            }, ct);

        if (Request.IsHtmx())
            return PartialView($"{ViewBase}/Partials/_PackageTable.cshtml", result.Data);

        ViewBag.Search = search;
        ViewBag.TypeFilter = type;
        return View($"{ViewBase}/Index.cshtml", result.Data);
    }

    [HttpGet("/matched-entities/{id:int}/detail")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct = default)
    {
        var result = await matchedEntityImportManager.GetPackageDetailAsync(id, ct);

        if (!result.Success)
        {
            if (Request.IsHtmx())
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = result.Message ?? "Paket bulunamadi.", type = "danger" });
                return StatusCode(404);
            }
            TempData.SetError(result.Message ?? "Paket bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        return PartialView($"{ViewBase}/Partials/_PackageDetail.cshtml", result.Data);
    }

    [HttpGet("/matched-entities/{id:int}/conflicts")]
    public async Task<IActionResult> Conflicts(int id, CancellationToken ct = default)
    {
        var conflictsResult = await matchedEntityImportManager.DetectConflictsAsync(id, ct);

        if (!conflictsResult.Success)
        {
            Response.HtmxTriggerWithData("showToast",
                new { message = conflictsResult.Message ?? "Cakisma tespiti basarisiz.", type = "danger" });
            return StatusCode(422);
        }

        var vm = new ConflictCheckVm
        {
            PackageId = id,
            Conflicts = conflictsResult.Data
        };

        return PartialView($"{ViewBase}/Partials/_ConflictResolution.cshtml", vm);
    }

    [HttpPost("/matched-entities/{id:int}/import")]
    public async Task<IActionResult> Import(int id, [FromForm] ImportFormModel form, CancellationToken ct = default)
    {
        var request = new ImportPackageRequest
        {
            PackageId = id,
            ConflictResolutions = form.Resolutions?.Select(r => new ConflictResolution
            {
                TemplateEntityId = r.TemplateEntityId,
                ExistingEntityId = r.ExistingEntityId,
                Strategy = r.Strategy
            }).ToList() ?? []
        };

        var result = await matchedEntityImportManager.ImportPackageAsync(request, ct);

        return HtmxMutationResult(result, "Paket basariyla import edildi.", "Import basarisiz.");
    }
}

/// <summary>
/// Form model for conflict resolution submissions.
/// </summary>
public record ImportFormModel
{
    public List<ConflictResolutionFormItem>? Resolutions { get; init; }
}

public record ConflictResolutionFormItem
{
    public int TemplateEntityId { get; init; }
    public int ExistingEntityId { get; init; }
    public ConflictResolutionStrategy Strategy { get; init; }
}

/// <summary>
/// ViewModel for the conflict resolution partial.
/// </summary>
public record ConflictCheckVm
{
    public int PackageId { get; init; }
    public List<ImportConflictDto> Conflicts { get; init; } = [];
}
