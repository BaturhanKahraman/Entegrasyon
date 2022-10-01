using Entegrasyon.Business.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CategoryAttributesController : ControllerBase
    {
        private readonly CategoryAttributeManager _categoryattributeManager;

        public CategoryAttributesController(CategoryAttributeManager categoryattributeManager)
        {
            _categoryattributeManager = categoryattributeManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryAttributes()
        {
            var result = await _categoryattributeManager.GetCategoryAttributes();
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetCategoryAttributesByCategory(int categoryId)
        {
            var result = await _categoryattributeManager.GetCategoryAttributesByCategory(categoryId);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
