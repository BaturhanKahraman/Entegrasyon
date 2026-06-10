using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Help;

[Authorize]
public class HelpController : Controller
{
    [HttpGet("/help")]
    public IActionResult Index()
    {
        ViewData.SetPageTitle("Yardım");
        ViewData.SetActiveNav("help");
        ViewData.SetBreadcrumb(("Yardım", null));

        // TODO: Yardım formu backend (ayrı task) — destek talebi gönderme,
        // SSS/dökümantasyon içeriği, admin paneline talep akışı burada bağlanacak.
        return View();
    }
}
