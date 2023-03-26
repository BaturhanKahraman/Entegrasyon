using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category;
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
        [HttpGet]
        public async Task<IActionResult> GetFavoriteCategories()
        {
            var result = await _categoryManager.GetFavoriteCategories();
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpGet]
        public async Task<IActionResult> GetSubCategories()
        {
            var result = await _categoryManager.GetSubCategories();
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromBody]int categoryId)
        {
            var result = await _categoryManager.AddFavorite(categoryId);
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryEditDetail(int id)
        {
            var result = await _categoryManager.GetCategoryEditDetail(id);
            return Ok(result);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetSuperCategories()
        {
            var result = await _categoryManager.GetSuperCategories();
            if(result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }

        [HttpPut]
        public async Task<IActionResult> EditCategory(EditCategoryDto dto)
        {
            var result = await _categoryManager.UpdateCategory(dto);
            if (result.Success)
                return Ok(result);
            return BadRequest(result.Message);
        }
    }
}
