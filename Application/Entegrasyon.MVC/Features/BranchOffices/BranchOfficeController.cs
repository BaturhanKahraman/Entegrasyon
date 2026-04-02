using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.BranchOffices;

[Authorize]
public class BranchOfficeController(IBranchOfficeManager branchOfficeManager) : Controller
{
    [HttpGet("/branch-offices")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Subeler");
        ViewData.SetActiveNav("branch-offices");

        var result = await branchOfficeManager.GetPageBranchListAsync();

        return View(result.Data);
    }

    [HttpGet("/branch-offices/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await branchOfficeManager.GetBranchDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Sube bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle(result.Data!.Name);
        ViewData.SetActiveNav("branch-offices");
        ViewData.SetBreadcrumb(("Subeler", "/branch-offices"), (result.Data.Name, null));

        var stocks = await branchOfficeManager.GetBranchStocksAsync(id);
        ViewBag.Stocks = stocks.Data;

        return View(result.Data);
    }

    [HttpPost("/branch-offices/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await branchOfficeManager.Delete(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Sube silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Sube basariyla silindi.");
        else
            TempData.SetError(result.Message ?? "Sube silinemedi.");

        return RedirectToAction(nameof(Index));
    }
}
