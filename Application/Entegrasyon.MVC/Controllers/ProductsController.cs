using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.MVC.Utility.Mapper;
using Entegrasyon.MVC.ViewModels.Products;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Shared.DTO;
using Shared.Entity;
namespace Entegrasyon.MVC.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ProductManager _productManager;

        public ProductsController(ProductManager productManager)
        {
            this._productManager = productManager;
        }

        // GET: ProductsController
        public async Task<ActionResult> Index([FromQuery]SearchablePageDto dto)
        {
            var result = await _productManager.GetProductsDetailsPageable(dto);
            // Updated line in the Index method to explicitly specify type arguments
            var model = result.Data.MapItems(Mappings.ToListViewModel);
            return View(model);
        }

        // GET: ProductsController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            if(string.IsNullOrEmpty(id))
                return BadRequest();
            bool isValid = Guid.TryParse(id,out var guidId);
            if(!isValid)
                return BadRequest();
            var result = await _productManager.GetProductDetailById(guidId);
            if(result.Data == null)
                return NotFound();
            return View(result.Data);
        }

        // GET: ProductsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ProductsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
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

        // GET: ProductsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ProductsController/Edit/5
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

        // GET: ProductsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ProductsController/Delete/5
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
