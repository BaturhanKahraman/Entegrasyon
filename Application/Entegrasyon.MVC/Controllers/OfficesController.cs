using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.ViewModels.Office;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Controllers
{
    [Authorize]
    public class OfficesController : Controller
    {
        // GET: OfficesController
        private readonly BranchOfficeManager _branchOfficeManager;
        public OfficesController(BranchOfficeManager branchOfficeManager)
        {
            _branchOfficeManager = branchOfficeManager;
        }

        public async Task<IActionResult> Index(int pageIndex=0,int pageSize=10)
        {
            var result =await _branchOfficeManager.GetPageableBranchOffices(pageIndex,pageSize);
            return View(result.Data);
        }

        // GET: OfficesController/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue || id == 0)
            {
                return BadRequest();
            }
            var dto = await _branchOfficeManager.GetBranchDetailById(id.Value);
            var model = dto.Data.Adapt<OfficeDetailViewModel>();
            return View(model);
        }

        // GET: OfficesController/Create
        [RestoreModelStateFromTempData]
        public ActionResult Create()
        {
            return View();
        }

        // POST: OfficesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SetTempDataModelState]
        public  async Task<IActionResult> Create(OfficeCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Create));
            var dto = model.Adapt<BranchOfficeAddDto>();
            var result = await _branchOfficeManager.AddBranch(dto);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty,result.Message);
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: OfficesController/Edit/5
        [RestoreModelStateFromTempData]
        public async Task<ActionResult> Edit(int? id)
        {
            if (!id.HasValue || id == 0)
                return BadRequest();
            var office = await _branchOfficeManager.GetBranchById(id.Value);
            var model = office.Adapt<OfficeEditViewModel>();
            return View(model);
        }
        
        [HttpPost]
        [SetTempDataModelState]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OfficeEditViewModel model)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Edit));
            var dto = model.Adapt<BranchOfficeEditDto>();
            var result = await _branchOfficeManager.Update(dto);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty,result.Message);
                return RedirectToAction(nameof(Edit));
            }
            return RedirectToAction(nameof(Details), new {result.Data.Id });

        }

        // POST: OfficesController/Delete/5
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Delete(IFormCollection fc)
        {
            if(!fc.ContainsKey("id"))
                return BadRequest();
            int id = Convert.ToInt32(fc["id"]);
            var result =await _branchOfficeManager.Delete(id);
            return Json(result);
        }
    }
}
