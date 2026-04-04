using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.MVC.Features.GiftCards.ViewModels;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.GiftCards;

[Authorize]
public class GiftCardController(IStorefrontGiftCardManager giftCardManager) : HtmxController
{
    private const int DefaultTenantId = 1;

    [HttpGet("/gift-cards")]
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewData.SetPageTitle("Hediye Kartlari");
        ViewData.SetActiveNav("gift-cards");

        var result = await giftCardManager.GetGiftCardsAsync(DefaultTenantId, page - 1, 20);

        if (Request.IsHtmx())
            return PartialView("Partials/_GiftCardTable", result.Data);

        return View(result.Data);
    }

    [HttpGet("/gift-cards/create")]
    public IActionResult Create()
    {
        ViewData.SetPageTitle("Yeni Hediye Karti");
        ViewData.SetActiveNav("gift-cards");
        ViewData.SetBreadcrumb(("Hediye Kartlari", "/gift-cards"), ("Yeni Karti", null));
        return View(new GiftCardCreateVm());
    }

    [HttpPost("/gift-cards/create")]
    public async Task<IActionResult> Create([FromForm] GiftCardCreateVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await giftCardManager.CreateGiftCardAsync(
            DefaultTenantId,
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
        var cardResult = await giftCardManager.GetByIdAsync(DefaultTenantId, id);

        if (!cardResult.Success)
        {
            TempData.SetError(cardResult.Message ?? "Hediye karti bulunamadi.");
            return RedirectToAction(nameof(Index));
        }

        var transactionsResult = await giftCardManager.GetTransactionsAsync(id);

        ViewData.SetPageTitle($"Hediye Karti: {cardResult.Data.Code}");
        ViewData.SetActiveNav("gift-cards");
        ViewData.SetBreadcrumb(("Hediye Kartlari", "/gift-cards"), (cardResult.Data.Code, null));

        ViewBag.Transactions = transactionsResult.Data ?? [];
        return View(cardResult.Data);
    }
}
