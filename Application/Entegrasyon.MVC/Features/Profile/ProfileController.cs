using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.MVC.Infrastructure.Extensions;
using System.Security.Claims;

namespace Entegrasyon.MVC.Features.Profile;

[Authorize]
public class ProfileController : Controller
{
    [HttpGet("/profile")]
    public IActionResult Index()
    {
        ViewData.SetPageTitle("Profil");
        ViewData.SetActiveNav("profile");

        ViewBag.UserName = User.Identity?.Name ?? "";
        ViewBag.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        ViewBag.FullName = User.FindFirstValue(ClaimTypes.GivenName) ?? "";

        return View();
    }
}
