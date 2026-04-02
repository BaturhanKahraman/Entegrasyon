using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Chat;

[Authorize]
public class ChatController : Controller
{
    [HttpGet("/chat")]
    public IActionResult Index()
    {
        ViewData.SetPageTitle("Mesajlar");
        ViewData.SetActiveNav("chat");
        return View();
    }
}
