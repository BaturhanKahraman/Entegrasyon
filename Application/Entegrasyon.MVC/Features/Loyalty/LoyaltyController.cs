using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Loyalty;

[Authorize]
public class LoyaltyController : HtmxController
{
    [HttpGet("/loyalty")]
    public IActionResult Index()
    {
        ViewData.SetPageTitle("Sadakat Programi");
        ViewData.SetActiveNav("loyalty");
        ViewData.SetBreadcrumb(("Sadakat Programi", null));

        return View();
    }
}
