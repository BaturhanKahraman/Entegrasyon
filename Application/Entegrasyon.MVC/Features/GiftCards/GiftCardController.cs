using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Entegrasyon.MVC.Features.GiftCards.ViewModels;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.GiftCards;

[Authorize]
public class GiftCardController(
    IStorefrontGiftCardManager giftCardManager,
    ITenantContext tenantContext) : HtmxController
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    [HttpGet("/gift-cards")]
    public async Task<IActionResult> Index(string? status = null, int page = 1)
    {
        ViewData.SetPageTitle("Hediye Kartlari");
        ViewData.SetActiveNav("gift-cards");

        GiftCardStatus? statusFilter = null;
        if (Enum.TryParse<GiftCardStatus>(status, out var parsed))
            statusFilter = parsed;

        var result = await giftCardManager.GetGiftCardsFilteredAsync(TenantId, statusFilter, page - 1, 20);

        ViewBag.Status = status;

        if (Request.IsHtmx())
            return PartialView("Partials/_GiftCardTable", result.Data);

        return View(result.Data);
    }

    [HttpGet("/gift-cards/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Hediye Karti");
        ViewData.SetActiveNav("gift-cards");
        ViewData.SetBreadcrumb(("Hediye Kartlari", "/gift-cards"), ("Yeni Kart", null));
        return View(new GiftCardCreateVm());
    }

    [HttpPost("/gift-cards/create")]
    public async Task<IActionResult> Create([FromForm] GiftCardCreateVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await giftCardManager.CreateGiftCardAsync(
            TenantId,
            vm.Amount,
            purchasedByCustomerId: null,
            vm.RecipientEmail,
            vm.RecipientName,
            vm.Message);

        if (result.Success)
        {
            TempData.SetSuccess($"Hediye karti basariyla olusturuldu. Kod: {result.Data.Code}");
            return RedirectToAction(nameof(Index));
        }

        TempData.SetError(result.Message ?? "Hediye karti olusturulamadi.");
        return View(vm);
    }

    [HttpGet("/gift-cards/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var cardResult = await giftCardManager.GetByIdAsync(TenantId, id);

        if (!cardResult.Success)
        {
            TempData.SetError(cardResult.Message ?? "Hediye karti bulunamadı.");
            return RedirectToAction(nameof(Index));
        }

        var transactionsResult = await giftCardManager.GetTransactionsAsync(id);

        ViewData.SetPageTitle($"Hediye Karti: {cardResult.Data.Code}");
        ViewData.SetActiveNav("gift-cards");
        ViewData.SetBreadcrumb(("Hediye Kartlari", "/gift-cards"), (cardResult.Data.Code, null));

        ViewBag.Transactions = transactionsResult.Data ?? [];
        return View(cardResult.Data);
    }

    [HttpPost("/gift-cards/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await giftCardManager.CancelGiftCardAsync(TenantId, id);
        return HtmxMutationResult(result, "Hediye karti iptal edildi.", refreshEvent: "giftCardChanged");
    }

    [HttpPost("/gift-cards/{id:int}/topup")]
    public async Task<IActionResult> TopUp(int id, [FromForm] decimal amount)
    {
        var result = await giftCardManager.TopUpGiftCardAsync(TenantId, id, amount);
        if (result.Success)
            return HtmxMutationResult(result, $"Bakiye yuklendi. Yeni bakiye: {result.Data:C2}", refreshEvent: "giftCardChanged");
        return HtmxMutationResult(result, result.Message ?? "Bakiye yuklenemedi.", refreshEvent: "giftCardChanged");
    }
}
