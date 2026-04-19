using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;
using Entegrasyon.MVC.Features.Settings.ViewModels;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Settings;

[Authorize(Roles = "Admin")]
public class ReceiptTemplateController(
    IReceiptTemplateManager templateManager,
    IReceiptRenderer receiptRenderer) : Controller
{
    [HttpGet("/settings/receipt-template")]
    public async Task<IActionResult> Index()
    {
        ViewData.SetPageTitle("Fiş Şablonu");
        ViewData.SetActiveNav("settings");

        var result = await templateManager.GetAsync();
        var vm = new ReceiptTemplateEditorVm { Template = result.Data ?? new ReceiptTemplateDto() };
        return View("~/Features/Settings/Views/ReceiptTemplate/Index.cshtml", vm);
    }

    [HttpPost("/settings/receipt-template")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateReceiptTemplateDto dto)
    {
        var result = await templateManager.UpdateAsync(dto);
        if (!result.Success)
        {
            Response.HtmxTriggerWithData("showToast", new { message = result.Message, level = "error" });
            return BadRequest(result.Message);
        }
        Response.HtmxTriggerWithData("showToast", new { message = "Şablon kaydedildi.", level = "success" });
        return Ok();
    }

    [HttpPost("/settings/receipt-template/upload-logo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        var result = await templateManager.UploadLogoAsync(file);
        return result.Success ? Ok(new { url = result.Data }) : BadRequest(result.Message);
    }

    [HttpDelete("/settings/receipt-template/logo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLogo()
    {
        var result = await templateManager.DeleteLogoAsync();
        return result.Success ? Ok() : BadRequest(result.Message);
    }

    [HttpPost("/settings/receipt-template/preview")]
    [ValidateAntiForgeryToken]
    public IActionResult Preview(
        [FromForm] string size,
        [FromForm] string mode,
        [FromForm] string? thermalJson,
        [FromForm] string? a4Json,
        [FromForm] string? logoUrl,
        [FromForm] int? logoWidthPx,
        [FromForm] string? storeName,
        [FromForm] string? storeAddress,
        [FromForm] string? storePhone)
    {
        var rMode = string.Equals(mode, "gift", StringComparison.OrdinalIgnoreCase) ? ReceiptMode.Gift : ReceiptMode.Normal;
        var rSize = string.Equals(size, "a4", StringComparison.OrdinalIgnoreCase) ? ReceiptSize.A4 : ReceiptSize.Thermal;

        var template = new ReceiptTemplateDto
        {
            ThermalJson = string.IsNullOrEmpty(thermalJson) ? "[]" : thermalJson,
            A4Json = string.IsNullOrEmpty(a4Json) ? "{}" : a4Json,
            LogoUrl = logoUrl,
            LogoWidthPx = logoWidthPx ?? 120,
            StoreName = storeName ?? "",
            StoreAddress = storeAddress ?? "",
            StorePhone = storePhone ?? ""
        };

        var html = receiptRenderer.RenderWithTemplate(template, BuildPlaceholderSale(), rMode, rSize);
        return Content(html, "text/html");
    }

    private static SaleDetailDto BuildPlaceholderSale() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        SaleNumber = "S20260419-0042",
        ReturnCode = "R-7K4QN9XM2P9AB",
        SaleDate = new DateTimeOffset(2026, 4, 19, 14, 30, 0, TimeSpan.FromHours(3)),
        CustomerName = "Ahmet Yılmaz",
        SalePersonName = "Demo Kasiyer",
        BranchOfficeName = "Merkez Şube",
        SubTotal = 250m, VatTotal = 50m, GrandTotal = 300m,
        Items =
        [
            new SaleDetailItemDto { Id = Guid.NewGuid(), ProductTitle = "Örnek Ürün A", Barcode = "1234567890123", Quantity = 1, UnitPriceWithVat = 120m, VatRate = 20m, LineTotalWithVat = 120m },
            new SaleDetailItemDto { Id = Guid.NewGuid(), ProductTitle = "Örnek Ürün B", Barcode = "9876543210987", Quantity = 2, UnitPriceWithVat = 90m, VatRate = 20m, LineTotalWithVat = 180m }
        ],
        Payments = [ new SaleDetailPaymentDto { PaymentMethodName = "Nakit", Amount = 300m, PaidAt = DateTimeOffset.UtcNow } ],
        VatSummary = [ new VatSummaryLineDto(20m, 250m, 50m, 300m) ]
    };
}
