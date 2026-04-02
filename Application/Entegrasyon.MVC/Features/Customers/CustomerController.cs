using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Customers;

[Authorize]
public class CustomerController(ICustomerManager customerManager) : Controller
{
    [HttpGet("/customers")]
    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewData.SetPageTitle("Musteriler");
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
        var result = await customerManager.GetCustomerDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Musteri bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle(result.Data!.NameSurname ?? result.Data.CorporateName);
        ViewData.SetActiveNav("customers");
        ViewData.SetBreadcrumb(("Musteriler", "/customers"), ("Detay", null));
        return View(result.Data);
    }

    [HttpGet("/customers/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Musteri");
        ViewData.SetActiveNav("customers");
        ViewData.SetBreadcrumb(("Musteriler", "/customers"), ("Yeni Musteri", null));
        return View();
    }

    [HttpPost("/customers/create")]
    public async Task<IActionResult> Create([FromForm] CustomerAddDto dto)
    {
        var result = await customerManager.AddCustomer(dto);

        if (result.Success)
        {
            TempData.SetSuccess("Musteri basariyla eklendi.");
            return RedirectToAction(nameof(Index));
        }

        TempData.SetError(result.Message ?? "Musteri eklenemedi.");
        return View(dto);
    }

    [HttpGet("/customers/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await customerManager.GetCustomerDetailById(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Musteri bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        ViewData.SetPageTitle("Musteri Duzenle");
        ViewData.SetActiveNav("customers");
        ViewData.SetBreadcrumb(("Musteriler", "/customers"), ("Duzenle", null));
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
            TempData.SetSuccess("Musteri basariyla guncellendi.");
            return RedirectToAction(nameof(Index));
        }

        TempData.SetError(result.Message ?? "Musteri guncellenemedi.");
        return RedirectToAction(nameof(Edit), new { id });
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
                    new { message = "Musteri silindi.", type = "success" });
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Silinemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Musteri basariyla silindi.");
        else
            TempData.SetError(result.Message ?? "Musteri silinemedi.");

        return RedirectToAction(nameof(Index));
    }
}
