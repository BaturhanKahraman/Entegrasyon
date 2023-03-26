using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Entegrasyon.MVC.Controllers
{
    [Route("/category/category-import")]
    public class CategoryImportController : Controller
    {
        private readonly TrendyolCategoryImporterService _trendyolCategoryImporterService;

        public CategoryImportController(TrendyolCategoryImporterService trendyolCategoryImporterService)
        {
            _trendyolCategoryImporterService = trendyolCategoryImporterService;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var result = await _trendyolCategoryImporterService.GetTrendyolCategories();
            if (result.Data == null)
            {
                return NoContent();
            }
            return View(result.Data.ToList());
        }
        [HttpPost]
        public async Task<IActionResult> ImportFromTrendyol()
        {
            var jsonTrendyolCategories = Request.Form["trendyolCategories"];
            var model = JsonConvert.DeserializeObject<List<ImportedTrendyolCategory>>(jsonTrendyolCategories);
            var selectedCategoryIds = Request.Form["selectedCategories"];
            var ids = selectedCategoryIds[0].Split(',').Select(x => Convert.ToInt32(x)).ToList();
            var flattenedCategoryIds = new List<int>();
            foreach(var category in model)
            {
                FlattenCategoryList(model,category,flattenedCategoryIds);
            }
            
            return View(nameof(Index),model);

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
