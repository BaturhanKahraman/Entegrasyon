using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TrendyolCategoriesController : ControllerBase
    {
        private readonly TrendyolCategoryImporterService _trendyolCategoryImporterService;

        public TrendyolCategoriesController(TrendyolCategoryImporterService trendyolCategoryImporterService)
        {
            _trendyolCategoryImporterService = trendyolCategoryImporterService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrendyolCategories()
        {
            var result = await _trendyolCategoryImporterService.GetTrendyolCategories();
            return Ok(result);
        }

        [HttpPost]
        public IActionResult ImportTrendyolCategories(List<TrendyolImport> imports)
        {
            var result = _trendyolCategoryImporterService.QueueImportingTrendyolCategories(imports);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
