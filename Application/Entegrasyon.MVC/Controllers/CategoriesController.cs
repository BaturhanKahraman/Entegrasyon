using AutoMapper;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.Utility.Attributes;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.Utility.Constants;
using Entegrasyon.MVC.ViewModels.Category;
using Entegrasyon.MVC.ViewModels.Customer;
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
        // GET: CategoriesController
        private readonly CategoryManager _categoryManager;
        private readonly IMapper _mapper;
        public CategoriesController(CategoryManager categoryManager, IMapper mapper)
        {
            _categoryManager = categoryManager;
            _mapper = mapper;
        }
        [Breadcrumb("Liste")]
        public async Task<ActionResult> Index(int pageIndex = 0, int pageSize = 10)
        {
            var result = await _categoryManager.GetCategoryDetailPageable(pageIndex, pageSize);
            var modelData = _mapper.Map<Pageable<CategoryDetailListViewModel>>(result.Data);
            return View(modelData);
        }

        // GET: CategoriesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CategoriesController/Create
        [RestoreModelStateFromTempData]
        [Breadcrumb("Oluştur")]
        public async Task<ActionResult> Create()
        {
            var model = new CategoryUpsertViewModel();
            await AddIfNotSuperCategoriesExists(model);
            return View(model);
        }


        // POST: CategoriesController/Create
        [HttpPost]
        [SetTempDataModelState]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CategoryUpsertViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Create), model);
            }
            var dto = _mapper.Map<AddCategoryDto>(model);
            var result = await _categoryManager.AddCategory(dto);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return RedirectToAction(nameof(Create));
            }
            ViewData[StringConstant.SuccessAlert] = result.Message;
            return RedirectToAction(nameof(Index));
        }


        // GET: CategoriesController/Edit/5
        [RestoreModelStateFromTempData]
        [Breadcrumb("Düzenle")]
        public async Task<ActionResult> Edit(int id)
        {
            var result = await _categoryManager.GetCategoryEditDetail(id);
            if (result.Data == null || !result.Success)
                return BadRequest();
            var model = _mapper.Map<CategoryUpsertViewModel>(result.Data);
            await AddIfNotSuperCategoriesExists(model);
            return View(model);
        }

        // POST: CategoriesController/Edit/5
        [HttpPost]
        [SetTempDataModelState]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CategoryUpsertViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Edit),model);
            var editDto = _mapper.Map<EditCategoryDto>(model);
            var result = await _categoryManager.UpdateCategory(editDto);
            if (result.Success)
            {
                ViewData[StringConstant.SuccessAlert]=result.Message;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", result.Message);
            return RedirectToAction(nameof(Edit));
        }


        // POST: CategoriesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _categoryManager.SoftDelete(id);
            if (!result.Success)
                return BadRequest(result.Message);
            ViewData[StringConstant.SuccessAlert] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        private async Task AddIfNotSuperCategoriesExists(CategoryUpsertViewModel model)
        {
            if(!model.SuperCategories.Any())
                model.SuperCategories.AddRange(
                    (await _categoryManager.GetSuperCategories()).Data
                    .Select(x => new SelectListItem(x.Name, x.Id.ToString())));
        }

    }
}
