using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.Utility.Attributes;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.Utility.Constants;
using Entegrasyon.MVC.ViewModels.Category;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shared.Entity;

namespace Entegrasyon.MVC.Controllers
{
    [Authorize]
    [Breadcrumb("Kategori", ViewModels.BreadcrumbUsageType.Controller)]
    public class CategoriesController : Controller
    {
        private readonly CategoryManager _categoryManager;

        public CategoriesController(CategoryManager categoryManager)
        {
            _categoryManager = categoryManager;
        }

        [Breadcrumb("Liste")]
        public async Task<ActionResult> Index(int pageIndex = 0,int pageSize = 10)
        {
            var result = await _categoryManager.GetCategoryDetailPageable(pageIndex,pageSize);
            var modelData = result.Data.Adapt<Pageable<CategoryDetailListViewModel>>();
            return View(modelData);
        }

        [RestoreModelStateFromTempData]
        [Breadcrumb("Oluştur")]
        public async Task<ActionResult> Create()
        {
            var model = new CategoryUpsertViewModel();
            await AddIfNotSuperCategoriesExists(model);
            return View(model);
        }

        [HttpPost]
        [SetTempDataModelState]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CategoryUpsertViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Create),model);
            }
            var dto = model.Adapt<AddCategoryDto>();
            var result = await _categoryManager.AddCategory(dto);
            if(!result.Success)
            {
                ModelState.AddModelError(string.Empty,result.Message);
                return RedirectToAction(nameof(Create));
            }
            ViewData[StringConstant.SuccessAlert] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [RestoreModelStateFromTempData]
        [Breadcrumb("Düzenle")]
        public async Task<ActionResult> Edit(int id)
        {
            var result = await _categoryManager.GetCategoryEditDetail(id);
            if(result.Data == null || !result.Success)
                return BadRequest();
            var model = result.Data.Adapt<CategoryUpsertViewModel>();
            await AddIfNotSuperCategoriesExists(model,id);
            return View(model);
        }

        [HttpPost]
        [SetTempDataModelState]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CategoryUpsertViewModel model)
        {
            if(!ModelState.IsValid)
                return RedirectToAction(nameof(Edit),model);
            var editDto = model.Adapt<EditCategoryDto>();
            var result = await _categoryManager.UpdateCategory(editDto);
            if(result.Success)
            {
                ViewData[StringConstant.SuccessAlert] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("",result.Message);
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _categoryManager.SoftDelete(id);
            if(!result.Success)
                return BadRequest(result.Message);
            ViewData[StringConstant.SuccessAlert] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        private async Task AddIfNotSuperCategoriesExists(CategoryUpsertViewModel model,int exceptId = 0)
        {
            if(!model.SuperCategories.Any())
                model.SuperCategories.AddRange(
                    (await _categoryManager.GetSuperCategories()).Data
                    .Where(c => c.Id != exceptId)
                    .Select(x => new SelectListItem(x.Name,x.Id.ToString())));
        }
    }
}
