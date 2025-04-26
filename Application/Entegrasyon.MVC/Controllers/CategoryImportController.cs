using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Entegrasyon.MVC.Utility.Attributes;
using Entegrasyon.MVC.Utility.Extensions;
using Entegrasyon.MVC.ViewModels.CategoryImport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Immutable;
using System.Text;

namespace Entegrasyon.MVC.Controllers
{
    [Route("/category/category-import")]
    [Authorize]
    [Breadcrumb("Kategori Aktarımı",ViewModels.BreadcrumbUsageType.Controller)]
    public class CategoryImportController(ITrendyolCategoryImportService trendyolCategoryImporterService)
        : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        [Route("GetTrendyolCategories")]
        public async Task<IActionResult> GetTrendyolCategories()
        {
            var categoriesResult = await trendyolCategoryImporterService.GetTrendyolCategories();
            if(categoriesResult.Success)
                return Json(categoriesResult.Data.ToJsTreeList());
            return NotFound(categoriesResult.Message);
        }
        [HttpPost]
        [Route("ImportFromTrendyol")]
        public async Task<IActionResult> ImportFromTrendyol([FromBody]List<TrendyolImportViewModel> model)
        {
            if (model == null || model.Count == 0)
                return BadRequest("Lütfen en az bir kategori seçin.");
            model.ForEach(x =>
            {
                if (x.Parent == "#")
                    x.Parent = string.Empty;
            });
            var lookup = model.ToLookup(f => f.Parent);

            model.ForEach(c =>
            {
                c.Children.AddRange(lookup[c.Id]);
            });

            List<TrendyolImportViewModel> hierarchical =
                model.Where(x => !model.Select(s => s.Id).Contains(x.Parent)).ToList();

            ImmutableList<TrendyolSelectedCategory> categories = [.. hierarchical.Select(h => h.ToTrendyolSelectedCategory())];

            await trendyolCategoryImporterService.QueueImporting(categories);

            return Ok();
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
