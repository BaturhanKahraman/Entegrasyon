using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Entegrasyon.MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Entegrasyon.MVC.Utility.Attributes;

namespace Entegrasyon.MVC.Controllers
{
    [Breadcrumb("Anasayfa",BreadcrumbUsageType.Controller)]
    [Route("Home")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _hostEnvironment;

        public HomeController(ILogger<HomeController> logger,IWebHostEnvironment webHost)
        {
            _logger = logger;
            _hostEnvironment = webHost;
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

        [Route("error")]
        public IActionResult Error(int code)
        {
            if(code == 0)
                return View();
            switch(code / 100)  // Bu, kodun ilk rakamını almamızı sağlar.
            {
                case 4:
                    ViewBag.ErrorMessage = "Muhtemelen sizden kaynaklı bir hata oluştu.";
                    break;

                case 5:
                    ViewBag.ErrorMessage = "Bizden kaynaklı bir hata oluştu, geliştiricilere haber verildi.";
                    break;
            }
            return View();
        }

        public IActionResult DemoPurpose()
        {
            return Json(new { env = _hostEnvironment.EnvironmentName });
        }
    }
}