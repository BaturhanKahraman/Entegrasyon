using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.POS;

[Authorize]
public class POSController : Controller
{
    [HttpGet("/pos")]
    public IActionResult Index()
    {
        ViewData.SetPageTitle("POS Terminali");
        ViewData.SetActiveNav("pos");
        return View();
    }
}
