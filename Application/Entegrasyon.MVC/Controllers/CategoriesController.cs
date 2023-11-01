using AutoMapper;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.Utility.Attributes;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.Utility.Constants;
using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Entity;

namespace Entegrasyon.MVC.Controllers
{
    [Breadcrumb("Kategori",ViewModels.BreadcrumbUsageType.Controller)]
    public class CategoriesController : Controller
    {
        // GET: CategoriesController
        private readonly CategoryManager _categoryManager;
        private readonly IMapper _mapper;
        public CategoriesController(CategoryManager categoryManager,IMapper mapper)
        {
            _categoryManager = categoryManager;
            this._mapper = mapper;
        }
        [Breadcrumb("Liste")]
        public async Task<ActionResult> Index(int pageIndex = 0,int pageSize = 10)
        {
            var result = await _categoryManager.GetCategoryDetailPageable(pageIndex,pageSize);
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
        public ActionResult Create()
        {
            return View();
        }

        // POST: CategoriesController/Create
        [HttpPost]
        [SetTempDataModelState]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CategoryAddViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Create),model);
            }
            var dto = _mapper.Map<AddCategoryDto>(model);
            var result = await _categoryManager.AddCategory(dto);
            if(!result.Success)
            {
                ModelState.AddModelError(string.Empty,result.Message);
                return RedirectToAction(nameof(Create));
            }
            ViewData[StringConstant.SuccessAlert] = result.Message;
            return RedirectToAction(nameof(Index));
        }


        // GET: CategoriesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: CategoriesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id,IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CategoriesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: CategoriesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id,IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
