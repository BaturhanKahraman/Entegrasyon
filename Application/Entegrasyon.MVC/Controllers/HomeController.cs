using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Entegrasyon.MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Entegrasyon.MVC.Utility.Attributes;

namespace Entegrasyon.MVC.Controllers
{
    [Breadcrumb("Anasayfa",BreadcrumbUsageType.Controller)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Breadcrumb("Gizlilik")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0,Location = ResponseCacheLocation.None,NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult DemoPurpose()
        {
            return ViewComponent("DemoViewComponent");
        }
    }
}