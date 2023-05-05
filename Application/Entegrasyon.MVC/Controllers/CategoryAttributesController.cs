using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Controllers
{
    public class CategoryAttributesController : Controller
    {
        private readonly CategoryManager _categoryManager;
        private readonly CategoryAttributeManager _categoryAttributeManager;
        private readonly static List<CategoryAttributeCreateViewModel> vm =
            new(2)
        {
            new CategoryAttributeCreateViewModel(){
                CategoryAttributeKey="Key 1" },
                new CategoryAttributeCreateViewModel(){
                                 AllowCustom=true,
                CategoryAttributeKey="Key 2",
                CategoryAttributeValues=new List<CategoryAttributeValueViewModel>(2){
                new CategoryAttributeValueViewModel(null,"Value 3"),
                new CategoryAttributeValueViewModel(null,"Value 4"),
                }
        } };
        public CategoryAttributesController(CategoryManager categoryManager,CategoryAttributeManager categoryAttributeManager)
        {
            _categoryManager = categoryManager;
            _categoryAttributeManager = categoryAttributeManager;
        }

        [HttpGet]
        [RestoreModelStateFromTempData]
        public async Task<IActionResult> Create(int? categoryId)
        {
            if(categoryId.HasValue)
            {
                if(!await _categoryManager.Exits(categoryId.Value))
                    return BadRequest();
                var categoryName = await _categoryManager.GetCategoryNameById(categoryId.Value);
                ViewBag.CategoryName = categoryName;
            }
            var model = new CategoryAttributeAddViewModel { CategoryId = null,CategoryAttributeList = vm };
            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetAllCategoryAttributes()
        {
            var results = await _categoryAttributeManager.GetCategoryAttributes();
            return Json(results.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SetTempDataModelState]
        public async Task<IActionResult> Create(CategoryAttributeAddViewModel model)
        {
            if(!ModelState.IsValid)
                return RedirectToAction(nameof(Create));
            int catId;
            string categoryQueryString = Request.Query["categoryId"].FirstOrDefault();
            if(!string.IsNullOrEmpty(categoryQueryString))
            {
                bool catBool = int.TryParse(categoryQueryString,out catId);
                if(!catBool || !await _categoryManager.Exits(catId))
                {
                    return BadRequest();
                }
            }

            return RedirectToAction(nameof(Create));
        }


    }
}
