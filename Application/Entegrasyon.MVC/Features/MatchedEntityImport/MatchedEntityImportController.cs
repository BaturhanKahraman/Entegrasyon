using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.MatchedEntityImport;

[Authorize]
public class MatchedEntityImportController(IMatchedEntityImportManager matchedEntityImportManager) : Controller
{
    private const string ViewBase = "~/Features/MatchedEntityImport/Views";

    [HttpGet("/matched-entities")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Eslestirilmis Varlik Aktarimi");
        ViewData.SetActiveNav("matched-entities");

        var result = await matchedEntityImportManager.GetAvailablePackagesAsync();
        return View($"{ViewBase}/Index.cshtml", result.Data);
    }
}
