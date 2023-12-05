using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.MVC.Utility.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Entegrasyon.MVC.Controllers
{
    [Route("/category/category-import")]
    [Authorize]
    [Breadcrumb("Kategori Aktarımı",ViewModels.BreadcrumbUsageType.Controller)]
    public class CategoryImportController : Controller
    {
        private readonly TrendyolCategoryImporterService _trendyolCategoryImporterService;

        public CategoryImportController(TrendyolCategoryImporterService trendyolCategoryImporterService)
        {
            _trendyolCategoryImporterService = trendyolCategoryImporterService;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        [Route("GetTrendyolCategories")]
        public async Task<IActionResult> GetTrendyolCategories()
        {
            var categoriesResult = await _trendyolCategoryImporterService.GetTrendyolCategories();
            if(categoriesResult.Success)
                return Json(categoriesResult.Data);
            return NotFound(categoriesResult.Message);
        }
        [HttpPost]
        public async Task<IActionResult> ImportFromTrendyol()
        {
            var selectedCategoryIds = Request.Form["selectedCategories"];
            var ids = selectedCategoryIds[0].Split(',').Select(x => Convert.ToInt32(x)).ToList();
            await Task.Yield();
            return RedirectToAction(nameof(Index));

        }
        [NonAction]
        private static void FlattenCategoryList(List<ImportedTrendyolCategory> categories,ImportedTrendyolCategory category,List<int> flatList)
        {
            flatList.Add(category.Id);
            if(category.SubCategories != null)
            {
                foreach(var child in category.SubCategories)
                {
                    FlattenCategoryList(categories,child,flatList);
                }
            }
        }
    }
}
