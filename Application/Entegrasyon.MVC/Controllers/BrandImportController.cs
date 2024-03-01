using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.MVC.Utility.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Controllers
{
    [Breadcrumb("Marka")]
    public class BrandImportController(TrendyolBrandImporterService trendyolBrandImporterService) : Controller
    {
        [Breadcrumb("İçe Aktarım")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]

        public async Task<IActionResult> GetTrendyolBrands()
        {
            var result = await trendyolBrandImporterService.QueueImporting();
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
