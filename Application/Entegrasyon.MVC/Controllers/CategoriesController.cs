using Entegrasyon.Business.Concrete;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Entity;

namespace Entegrasyon.MVC.Controllers
{
    public class CategoriesController : Controller
    {
        // GET: CategoriesController
        private readonly CategoryManager _categoryManager;

        public CategoriesController(CategoryManager categoryManager)
        {
            _categoryManager = categoryManager;
        }

        public async Task<ActionResult> Index(int pageIndex = 0,int pageSize = 10)
        {
            var model = await _categoryManager.GetCategoryDetailPageable(pageIndex,pageSize);
            return View(model.Data);
        }

        // GET: CategoriesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CategoriesController/Create
        [RestoreModelStateFromTempData]
        public ActionResult Create()
        {
            return View();
        }

        // POST: CategoriesController/Create
        [HttpPost]
        [SetTempDataModelState]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryAddViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Create),model);
            }
            return View();
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
