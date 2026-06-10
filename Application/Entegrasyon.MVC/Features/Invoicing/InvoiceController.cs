using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Invoicing;

[Authorize]
public class InvoiceController(IEInvoiceManager invoiceManager) : Controller
{
    [HttpGet("/invoicing")]
    public async Task<IActionResult> Index(
        string? search = null,
        EInvoiceStatus? status = null,
        EInvoiceType? type = null,
        int page = 1)
    {
        ViewData.SetPageTitle("Faturalar");
        ViewData.SetActiveNav("invoicing");

        var filter = new EInvoiceFilterDto
        {
            PageIndex = page - 1,
            PageSize = 20,
            Status = status,
            InvoiceType = type,
            SearchTerm = search
        };

        var result = await invoiceManager.GetInvoices(filter);
        var pageable = result.Data;

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.InvoiceType = type;

        if (Request.IsHtmx())
            return PartialView("~/Features/Invoicing/Views/Partials/_InvoiceTable.cshtml", pageable);

        return View("~/Features/Invoicing/Views/Index.cshtml", pageable);
    }

    [HttpGet("/invoicing/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var result = await invoiceManager.GetInvoiceDetail(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "Fatura bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var invoice = result.Data!;

        ViewData.SetPageTitle($"Fatura #{invoice.InvoiceNumber}");
        ViewData.SetActiveNav("invoicing");
        ViewData.SetBreadcrumb(("Faturalar", "/invoicing"), ($"#{invoice.InvoiceNumber}", null));

        return View("~/Features/Invoicing/Views/Detail.cshtml", invoice);
    }

    [HttpGet("/invoicing/create")]
    public IActionResult Create(Guid? saleId = null)
    {
        ViewData.SetPageTitle("Yeni Fatura");
        ViewData.SetActiveNav("invoicing");
        ViewData.SetBreadcrumb(("Faturalar", "/invoicing"), ("Yeni Fatura", null));

        ViewBag.SaleId = saleId;
        return View("~/Features/Invoicing/Views/Create.cshtml");
    }

    [HttpPost("/invoicing/create")]
    public async Task<IActionResult> Create([FromForm] CreateEInvoiceDto dto)
    {
        var result = await invoiceManager.CreateInvoice(dto);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Fatura basariyla olusturuldu.", type = "success" });
                Response.HtmxRedirect($"/invoicing/{result.Data}");
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Fatura olusturulamadi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
        {
            TempData.SetSuccess("Fatura basariyla olusturuldu.");
            return Redirect($"/invoicing/{result.Data}");
        }

        TempData.SetError(result.Message ?? "Fatura olusturulamadi.");
        return RedirectToAction(nameof(Create));
    }

    [HttpPost("/invoicing/{id:guid}/send")]
    public async Task<IActionResult> SendToGib(Guid id)
    {
        var result = await invoiceManager.SendToGib(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Fatura GIB'e gonderildi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Fatura gonderilemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Fatura GIB'e basariyla gonderildi.");
        else
            TempData.SetError(result.Message ?? "Fatura GIB'e gonderilemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/invoicing/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await invoiceManager.CancelInvoice(id);

        if (Request.IsHtmx())
        {
            if (result.Success)
            {
                Response.HtmxTriggerWithData("showToast",
                    new { message = "Fatura iptal edildi.", type = "success" });
                Response.HtmxRefresh();
                return Content("");
            }

            Response.HtmxTriggerWithData("showToast",
                new { message = result.Message ?? "Fatura iptal edilemedi.", type = "danger" });
            return StatusCode(422);
        }

        if (result.Success)
            TempData.SetSuccess("Fatura basariyla iptal edildi.");
        else
            TempData.SetError(result.Message ?? "Fatura iptal edilemedi.");

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("/invoicing/bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkInvoiceDto dto)
    {
        var result = await invoiceManager.CreateBulkInvoices(dto);
        if (!result.Success)
        {
            Response.StatusCode = 422;
            return Json(new { message = result.Message });
        }

        TempData.SetSuccess($"{result.Data!.Count} fatura basariyla olusturuldu.");
        return Json(new { redirect = "/invoicing" });
    }

    [HttpGet("/invoicing/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        var result = await invoiceManager.DownloadPdf(id);
        if (!result.Success)
        {
            TempData.SetError(result.Message ?? "PDF indirilemedi.");
            return RedirectToAction(nameof(Detail), new { id });
        }

        return File(result.Data!, "application/pdf", $"fatura-{id}.pdf");
    }
}
