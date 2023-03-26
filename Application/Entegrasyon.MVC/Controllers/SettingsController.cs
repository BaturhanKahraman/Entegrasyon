using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Controllers
{
    public class SettingsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
