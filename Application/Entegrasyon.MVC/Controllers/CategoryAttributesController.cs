using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.MVC.Utility.Attributes;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.Utility.Constants;
using Entegrasyon.MVC.ViewModels;
using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Controllers
{
    [Breadcrumb("Kategori Özelliği", BreadcrumbUsageType.Controller)]
    [Route("CategoryAttributes")]
    public class CategoryAttributesController : Controller
    {
        private readonly CategoryManager _categoryManager;
        private readonly CategoryAttributeManager _categoryAttributeManager;
        
        public CategoryAttributesController(CategoryManager categoryManager, CategoryAttributeManager categoryAttributeManager)
        {
            _categoryManager = categoryManager;
            _categoryAttributeManager = categoryAttributeManager;
        }

        [HttpGet]
        [Breadcrumb("Ekle")]
        [RestoreModelStateFromTempData]
        [Route("Upsert/{categoryId:int}")]
        public async Task<IActionResult> Upsert(int? categoryId)
        {
            if (categoryId.HasValue)
            {
                if (!await _categoryManager.Exits(categoryId.Value))
                    return BadRequest("Böyle bir kategori bulunmamaktadır.");
                if (await _categoryManager.IsSuper(categoryId))
                    return BadRequest("Bu kategorinin alt kategorileri var. Özellik ekleme yapamazsınız.");
                var categoryName = await _categoryManager.GetCategoryNameById(categoryId.Value);
                ViewBag.CategoryName = categoryName;
                ViewBag.CategoryId = categoryId.Value;
                var model = new CategoryAttributeAddViewModel()
                {
                    CategoryId = categoryId.Value,
                };
                var result = await _categoryAttributeManager.GetCategoryAttributesByCategory(categoryId.Value);
                if (result.Data == null)
                    return View(model);
                model.CategoryAttributeList = result.Data //mapper getir
                     .Select(ca => new CategoryAttributeCreateViewModel() { 
                            AllowCustom=ca.AllowCustom,
                            CategoryAttributeKey = ca.CategoryAttributeKey,
                            CategoryAttributeValues=ca.CategoryAttributeValues.Select(cav=>new CategoryAttributeValueViewModel(cav.Id,cav.Name))
                                .ToList(),
                            Id=ca.Id,
                            IsRequired=ca.IsRequired,
                            IsSlicer = ca.IsSlicer,
                            IsVarianter = ca.IsVarianter
                        })
                     .ToList();
                return View(model);//update ise isslicer ve isvarianterı değiştirememeli.
            }
            else
            {
                ViewData[StringConstant.WarningAlert] = "Bir hata oluştu";
                return RedirectToAction("Index", "Category");
            }
        }

        [HttpGet]
        public IActionResult SuccesffullyAdded()
        {
            ViewData[StringConstant.SuccessAlert] = "Kategori özellikleri başarıyla eklenmiştir.";
            return RedirectToAction("Index", "Categories");
        }

    }
}
