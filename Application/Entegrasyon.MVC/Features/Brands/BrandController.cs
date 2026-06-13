using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Requests;
using Entegrasyon.MVC.Features.Brands.ViewModels;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Brands;

[Authorize]
public class BrandController(
    IBrandService brandService,
    IBrandMatchService brandMatchService,
    IMasterCatalogImportService masterCatalogImportService,
    ITenantContext tenantContext) : HtmxController
{
    [HttpGet("/brands")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Markalar");
        ViewData.SetActiveNav("brands");

        var result = await brandService.GetBrandDetailPageable(
            new BrandDetailPaginatedRequest { SearchTerm = search, PageIndex = page - 1, PageSize = 20 });

        if (Request.IsHtmx())
            return PartialView("Partials/_BrandTable", result.Data);

        // KPI kartları yalnız tam-sayfa render'da (HTMX tablo refresh'inde gereksiz sorgu yok).
        var kpis = await brandService.GetBrandKpisAsync();
        ViewBag.TotalProductCount = kpis.TotalProductCount;
        ViewBag.MatchedBrandCount = kpis.MatchedBrandCount;
        ViewBag.BrandsWithoutProductCount = kpis.BrandsWithoutProductCount;

        ViewBag.Search = search;
        return View(result.Data);
    }

    [HttpPost("/brands/create")]
    public async Task<IActionResult> Create([FromForm] string name)
    {
        var result = await brandService.AddBrand(new AddBrandDto { Name = name });

        return HtmxMutationResult(result, "Marka eklendi.", "Eklenemedi.", refreshEvent: "brandChanged");
    }

    [HttpGet("/brands/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await brandService.GetBrandDetail(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Marka bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var mappings = await brandMatchService.GetBrandMappingsByBrandIdAsync(id);

        ViewData.SetPageTitle(result.Data!.Name);
        ViewData.SetActiveNav("brands");
        ViewData.SetBreadcrumb(("Markalar", "/brands"), (result.Data.Name, null));

        ViewBag.Mappings = mappings.Success ? mappings.Data : new List<Entity.Dtos.Brand.BrandMarketPlaceMatchDto>();
        return View(result.Data);
    }

    [HttpGet("/brands/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await brandService.GetBrandDetail(id);
        if (!result.Success || result.Data == null)
        {
            TempData.SetError("Marka bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle($"{result.Data.Name} — Düzenle");
        ViewData.SetActiveNav("brands");
        ViewData.SetBreadcrumb(("Markalar", "/brands"), (result.Data.Name, $"/brands/{id}"), ("Düzenle", null));

        return View(result.Data);
    }

    [HttpPost("/brands/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] string name, [FromForm] string? seoSlug)
    {
        var result = await brandService.UpdateBrand(new EditBrandDto(id, name, seoSlug));
        if (result.Success)
        {
            TempData.SetSuccess("Marka güncellendi.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        TempData.SetError(result.Message ?? "Güncellenemedi.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("/brands/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await brandService.DeleteBrand(id);

        return HtmxMutationResult(result, "Marka silindi.", "Silinemedi.");
    }

    // ── Master Import ────────────────────────────────────────────────

    [HttpGet("/brands/master-import")]
    public IActionResult MasterImport()
    {
        ViewData.SetPageTitle("Master Katalogdan Marka Aktarma");
        ViewData.SetActiveNav("brands");
        ViewData.SetBreadcrumb(("Markalar", "/brands"), ("Master Import", null));

        return View();
    }

    [HttpGet("/brands/master-import/search")]
    public async Task<IActionResult> MasterImportSearch([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return PartialView("Partials/_BrandSearchResults", Array.Empty<Entity.Dtos.MasterCatalog.MasterBrandDto>());

        var brands = await masterCatalogImportService.SearchMasterBrandsAsync(q.Trim());
        return PartialView("Partials/_BrandSearchResults", brands);
    }

    [HttpPost("/brands/master-import")]
    public async Task<IActionResult> MasterImportExecute([FromForm] int[] selectedBrandIds)
    {
        if (selectedBrandIds.Length == 0)
        {
            TempData.SetError("Lutfen en az bir marka secin.");
            return RedirectToAction(nameof(MasterImport));
        }

        try
        {
            var result = await masterCatalogImportService.ImportBrandsFromMasterAsync(
                tenantContext.TenantId, selectedBrandIds);

            var resultVm = new BrandMasterImportResultVm
            {
                Success = true,
                BrandsImported = result.BrandsImported,
                BrandsSkipped = result.BrandsSkipped
            };

            if (Request.IsHtmx())
                return PartialView("Partials/_BrandMasterImportResult", resultVm);

            TempData.SetSuccess($"{result.BrandsImported} marka basariyla aktarildi.");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (Request.IsHtmx())
            {
                return PartialView("Partials/_BrandMasterImportResult", new BrandMasterImportResultVm
                {
                    Success = false,
                    ErrorMessage = "Import sirasinda bir hata olustu: " + ex.Message
                });
            }

            TempData.SetError("Import sirasinda bir hata olustu.");
            return RedirectToAction(nameof(MasterImport));
        }
    }
}
