using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.DiscountVouchers;
using Entegrasyon.MVC.Features.Discounts.ViewModels;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Discounts;

[Authorize]
public class DiscountController(IDiscountVoucherManager discountVoucherManager) : HtmxController
{
    [HttpGet("/discounts")]
    public async Task<IActionResult> Index(string? code = null, int page = 1)
    {
        ViewData.SetPageTitle("İndirim Kodlari");
        ViewData.SetActiveNav("discounts");

        var result = await discountVoucherManager.GetDiscountVouchers(page - 1, 20, code);

        ViewBag.Code = code;

        if (Request.IsHtmx())
            return PartialView("Partials/_VoucherTable", result.Data);

        return View(result.Data);
    }

    [HttpGet("/discounts/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni İndirim Kodu");
        ViewData.SetActiveNav("discounts");
        ViewData.SetBreadcrumb(("İndirim Kodlari", "/discounts"), ("Yeni Kod", null));
        return View(new DiscountCreateVm());
    }

    [HttpPost("/discounts/create")]
    public async Task<IActionResult> Create([FromForm] DiscountCreateVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var dto = new CreateDiscountVoucherDto(vm.Amount, vm.ExpiringDate, vm.CustomerId);
        var result = await discountVoucherManager.CreateDiscountVoucher(dto);

        if (result.Success)
        {
            TempData.SetSuccess($"İndirim kodu basariyla olusturuldu: {result.Data}");
            return RedirectToAction(nameof(Index));
        }

        TempData.SetError(result.Message ?? "İndirim kodu olusturulamadi.");
        return View(vm);
    }

    [HttpPost("/discounts/{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id, [FromForm] bool activate)
    {
        var ids = new[] { id };
        var result = activate
            ? await discountVoucherManager.MakeActiveDiscountVouchers(ids)
            : await discountVoucherManager.MakePassiveDiscountVouchers(ids);

        return HtmxMutationResult(
            result,
            activate ? "İndirim kodu aktif edildi." : "İndirim kodu pasife alindi.",
            activate ? "İndirim kodu aktif edilemedi." : "İndirim kodu pasife alinamadi.",
            refreshEvent: "voucherToggled");
    }
}
