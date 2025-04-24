using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.MVC.Utility.Attributes.ModelState;
using Entegrasyon.MVC.ViewModels.Customer;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Controllers;

public class CustomersController : Controller
{
    // GET
    private readonly CustomerManager _customerManager;
    public CustomersController(CustomerManager customerManager)
    {
        _customerManager = customerManager;
    }

    public async Task<IActionResult> Index(string searchKey,int pageIndex = 0,int pageSize = 50)
    {
        var result = await _customerManager.GetCustomerDetailPageable(searchKey, pageIndex, pageSize);
        return View(result.Data);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null or 0)
        {
            return BadRequest();
        }

        var result =await _customerManager.GetCustomerDetailById(id.Value);
        var model = result.Data.Adapt<CustomerDetailViewModel>();
        
        return View(model);
    }

    // GET: OfficesController/Create
    [RestoreModelStateFromTempData]
    public ActionResult Add()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SetTempDataModelState]
    public async Task<IActionResult> Add(CustomerAddViewModel model)
    {
        if(!ModelState.IsValid)
            return RedirectToAction(nameof(Add));
        var dto = model.Adapt<CustomerAddDto>();
        var result = await _customerManager.AddCustomer(dto);
        if(!result.Success)
        {
            ModelState.AddModelError(string.Empty,result.Message);
            return RedirectToAction(nameof(Add));
        }

        return RedirectToAction(nameof(Index));
    }
    
    [RestoreModelStateFromTempData]
    public async Task<ActionResult> Edit(int? id)
    {
        if(id is null or 0)
            return BadRequest();
        var result = await _customerManager.GetCustomerById(id.Value);
        var model = result.Data.Adapt<CustomerEditViewModel>();
        return View(model);
    }

    [HttpPost]
    [SetTempDataModelState]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerEditViewModel model)
    {
        if(!ModelState.IsValid)
            return RedirectToAction(nameof(Edit));
        var dto = model.Adapt<UpdateCustomerDto>();
        var result = await _customerManager.UpdateCustomer(dto);
        if(!result.Success)
        {
            ModelState.AddModelError(string.Empty,result.Message);
            return RedirectToAction(nameof(Edit));
        }
        return RedirectToAction(nameof(Details),new { result.Data.Id });

    }
    
    [Authorize]
    [HttpPost]
    public async Task<ActionResult> Delete(IFormCollection fc)
    {
        if(!fc.ContainsKey("id"))
            return BadRequest();
        int id = Convert.ToInt32(fc["id"]);
        var result = await _customerManager.SoftDelete(id);
        return Json(result);
    }
}