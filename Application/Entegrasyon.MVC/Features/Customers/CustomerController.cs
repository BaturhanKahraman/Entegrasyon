using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Customers;

[Authorize]
public class CustomerController(ICustomerManager customerManager, IOrderManager orderManager) : Controller
{
    [HttpGet("/customers")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Müşteriler");
        ViewData.SetActiveNav("customers");

        var result = await customerManager.GetCustomerDetailPageable(
            search ?? "", page - 1, 20);

        if (Request.IsHtmx())
            return PartialView("Partials/_CustomerTable", result.Data);

        ViewBag.Search = search;
        return View(result.Data);
    }

    [HttpGet("/customers/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var customerResult = await customerManager.GetCustomerDetailById(id);
        if (!customerResult.Success || customerResult.Data == null)
        {
            TempData.SetError(customerResult.Message ?? "Müşteri bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var ordersResult = await orderManager.GetCustomerOrdersAsync(id, 1);
        var activityResult = await customerManager.GetCustomerActivity(id);

        var customer = customerResult.Data;
        var orders = ordersResult.Data ?? [];

        ViewBag.Orders = orders;
        ViewBag.TotalSpend = orders.Sum(o => o.GrossAmount ?? 0m);
        ViewBag.OrderCount = orders.Count;
        ViewBag.LastOrderDate = orders.Count > 0
            ? orders.Max(o => o.CreatedAt)
            : (DateTimeOffset?)null;
        ViewBag.AverageOrder = orders.Count > 0
            ? orders.Sum(o => o.GrossAmount ?? 0m) / orders.Count
            : 0m;
        ViewBag.Activities = activityResult.Data ?? [];

        ViewData.SetPageTitle(customer.NameSurname ?? customer.CorporateName);
        ViewData.SetActiveNav("customers");
        ViewData.SetBreadcrumb(("Müşteriler", "/customers"), ("Detay", null));
        return View(customer);
    }

    [HttpGet("/customers/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Müşteri");
        ViewData.SetActiveNav("customers");
        ViewData.SetBreadcrumb(("Müşteriler", "/customers"), ("Yeni Müşteri", null));
        return View();
    }

    [HttpPost("/customers/create")]
    public async Task<IActionResult> Create([FromForm] CustomerAddDto dto)
    {
        var result = await customerManager.AddCustomer(dto);

        if (result.Success)
        {
            TempData.SetSuccess("Müşteri başarıyla eklendi.");
            return RedirectToAction(nameof(Index));
        }

        TempData.SetError(result.Message ?? "Müşteri eklenemedi.");
        return View(dto);
    }

    [HttpGet("/customers/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await customerManager.GetCustomerDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Müşteri bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Müşteri Düzenle");
        ViewData.SetActiveNav("customers");
        ViewData.SetBreadcrumb(("Müşteriler", "/customers"), ("Düzenle", null));
        return View(result.Data);
    }

    [HttpPost("/customers/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, [FromForm] UpdateCustomerDto dto)
    {
        if (dto.Id != id)
            dto = dto with { Id = id };

        var result = await customerManager.UpdateCustomer(dto);

        if (result.Success)
        {
            TempData.SetSuccess("Müşteri başarıyla güncellendi.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        TempData.SetError(result.Message ?? "Müşteri güncellenemedi.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("/customers/{id:int}/set-active")]
    public async Task<IActionResult> SetActive(int id, [FromForm] bool active, [FromForm] string? reason = null)
    {
        var result = await customerManager.SetActive(id, active, reason);

        if (result.Success)
            TempData.SetSuccess(active ? "Müşteri aktif hale getirildi." : "Müşteri deaktif edildi.");
        else
            TempData.SetError(result.Message ?? "İşlem başarısız.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/customers/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await customerManager.SoftDelete(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Müşteri silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Müşteri başarıyla silindi.");
        else
            TempData.SetError(result.Message ?? "Müşteri silinemedi.");

        return RedirectToAction(nameof(Index));
    }
}
