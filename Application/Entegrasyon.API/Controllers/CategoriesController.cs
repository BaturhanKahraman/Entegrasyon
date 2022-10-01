using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {


        private readonly CategoryManager _categoryManager;

        public CategoriesController(CategoryManager categoryManager)
        {
            _categoryManager = categoryManager;
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(AddCategoryDto dto)
        {
            var result = await _categoryManager.AddCategory(dto);
            if (result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetCategoryDetails()
        {
            var result = await _categoryManager.GetCategoryDetailList();
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetCategoryDetailsPage(int page,int itemCount,string categoryName)
        {
            var result = await _categoryManager.GetCategoryDetailPageable(page,itemCount,categoryName);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        
    }
}
