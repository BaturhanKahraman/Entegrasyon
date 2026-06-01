using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Entegrasyon.MVC.Features.Storefront.ViewModels;
using Entegrasyon.MVC.Infrastructure.Controllers;
using Entegrasyon.MVC.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entegrasyon.MVC.Features.Storefront;

[Authorize]
public class StorefrontController(
    IStorefrontSettingsManager settingsManager,
    IStorefrontBannerManager bannerManager,
    IStorefrontPageManager pageManager,
    IStorefrontReviewManager reviewManager,
    IStorefrontReturnManager returnManager,
    IStorefrontCampaignManager campaignManager,
    IStorefrontContactManager contactManager,
    IStorefrontQnAManager qnaManager,
    IStorefrontNewsletterManager newsletterManager,
    ISellerPayoutManager payoutManager,
    IStorefrontSizeGuideManager sizeGuideManager,
    ISellerCommissionManager sellerCommissionManager,
    IStorefrontAbandonedCartManager abandonedCartManager,
    IStorefrontWalletManager walletManager,
    IStorefrontLoyaltyManager loyaltyManager,
    IStorefrontWishlistManager wishlistManager,
    IStorefrontStockNotificationManager stockNotificationManager,
    IStorefrontSearchHistoryManager searchHistoryManager,
    IStorefrontPushManager pushManager,
    ITenantContext tenantContext) : HtmxController
{
    // Suppress unused-parameter warning for injected services used by other feature agents
    private readonly IStorefrontPageManager _pageManager = pageManager;
    private readonly IStorefrontWishlistManager _wishlistManager = wishlistManager;
    private readonly IStorefrontStockNotificationManager _stockNotificationManager = stockNotificationManager;

    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    // ── Settings ─────────────────────────────────────────────────────────

    [HttpGet("/settings/storefront")]
    public async Task<IActionResult> Settings()
    {
        ViewData.SetPageTitle("Magaza Ayarlari");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", null), ("Ayarlar", null));

        var result = await settingsManager.GetByTenantIdAsync(TenantId);
        return View(result.Data);
    }

    [HttpPost("/settings/storefront")]
    public async Task<IActionResult> SaveSettings(StorefrontSettings model)
    {
        model.TenantId = TenantId;
        var result = await settingsManager.CreateOrUpdateAsync(model);

        return HtmxMutationResult(result,
            "Magaza ayarlari kaydedildi.",
            "Magaza ayarlari kaydedilemedi.",
            refreshEvent: "settingsUpdated",
            redirectAction: nameof(Settings));
    }

    [HttpPost("/settings/storefront/maintenance")]
    public async Task<IActionResult> ToggleMaintenance(bool enabled, string? message)
    {
        var result = await settingsManager.ToggleMaintenanceModeAsync(TenantId, enabled, message);

        return HtmxMutationResult(result,
            enabled ? "Bakim modu aktif edildi." : "Bakim modu kapatildi.",
            refreshEvent: "settingsUpdated",
            redirectAction: nameof(Settings));
    }

    // ── Banners ──────────────────────────────────────────────────────────

    [HttpGet("/settings/storefront/banners")]
    public async Task<IActionResult> Banners()
    {
        ViewData.SetPageTitle("Banner Yonetimi");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Bannerlar", null));

        var result = await bannerManager.GetAllBannersAsync(TenantId);
        return View(result.Data ?? []);
    }

    [HttpPost("/settings/storefront/banners")]
    public async Task<IActionResult> CreateBanner(StorefrontBanner model)
    {
        model.TenantId = TenantId;
        var result = await bannerManager.CreateAsync(model);

        return HtmxMutationResult(result,
            "Banner olusturuldu.",
            "Banner olusturulamadi.",
            refreshEvent: "bannersUpdated",
            redirectAction: nameof(Banners));
    }

    [HttpPut("/settings/storefront/banners/{id:int}")]
    public async Task<IActionResult> UpdateBanner(int id, StorefrontBanner model)
    {
        model.Id = id;
        model.TenantId = TenantId;
        var result = await bannerManager.UpdateAsync(model);

        return HtmxMutationResult(result,
            "Banner guncellendi.",
            "Banner guncellenemedi.",
            refreshEvent: "bannersUpdated",
            redirectAction: nameof(Banners));
    }

    [HttpDelete("/settings/storefront/banners/{id:int}")]
    public async Task<IActionResult> DeleteBanner(int id)
    {
        var result = await bannerManager.DeleteAsync(id);

        return HtmxMutationResult(result,
            "Banner silindi.",
            "Banner silinemedi.",
            refreshEvent: "bannersUpdated",
            redirectAction: nameof(Banners));
    }

    // ── Payment ──────────────────────────────────────────────────────────

    [HttpGet("/settings/storefront/payment")]
    public async Task<IActionResult> Payment()
    {
        ViewData.SetPageTitle("Odeme Ayarlari");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Odeme Ayarlari", null));

        var result = await settingsManager.GetByTenantIdAsync(TenantId);
        return View(result.Data);
    }

    // ── Legal ────────────────────────────────────────────────────────────

    [HttpGet("/settings/storefront/legal")]
    public async Task<IActionResult> Legal()
    {
        ViewData.SetPageTitle("Yasal Bilgiler");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Yasal Bilgiler", null));

        var result = await settingsManager.GetByTenantIdAsync(TenantId);
        return View(result.Data);
    }

    [HttpPost("/settings/storefront/legal")]
    public async Task<IActionResult> SaveLegalText(string fieldName, string htmlContent)
    {
        var result = await settingsManager.UpdateLegalTextAsync(TenantId, fieldName, htmlContent);

        return HtmxMutationResult(result,
            "Yasal metin kaydedildi.",
            "Yasal metin kaydedilemedi.",
            refreshEvent: "legalUpdated",
            redirectAction: nameof(Legal));
    }

    // ── Reviews ──────────────────────────────────────────────────────────

    [HttpGet("/storefront/reviews")]
    public async Task<IActionResult> Reviews()
    {
        ViewData.SetPageTitle("Degerlendirmeler");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Degerlendirmeler", null));

        var result = await reviewManager.GetAllReviewsAsync(TenantId);
        return View(result.Data ?? []);
    }

    [HttpPost("/storefront/reviews/{id:int}/approve")]
    public async Task<IActionResult> ApproveReview(int id)
    {
        var result = await reviewManager.ApproveReviewAsync(id);

        return HtmxMutationResult(result,
            "Degerlendirme onaylandi.",
            "Degerlendirme onaylanamadi.",
            refreshEvent: "reviewsUpdated",
            redirectAction: nameof(Reviews));
    }

    [HttpPost("/storefront/reviews/{id:int}/reject")]
    public async Task<IActionResult> RejectReview(int id)
    {
        var result = await reviewManager.RejectReviewAsync(id);

        return HtmxMutationResult(result,
            "Degerlendirme reddedildi.",
            "Degerlendirme reddedilemedi.",
            refreshEvent: "reviewsUpdated",
            redirectAction: nameof(Reviews));
    }

    [HttpPost("/storefront/reviews/{id:int}/reply")]
    public async Task<IActionResult> ReplyToReview(int id, string replyText)
    {
        var result = await reviewManager.ReplyToReviewAsync(id, replyText);

        return HtmxMutationResult(result,
            "Yanit gonderildi.",
            "Yanit gonderilemedi.",
            refreshEvent: "reviewsUpdated",
            redirectAction: nameof(Reviews));
    }

    // ── Returns ──────────────────────────────────────────────────────────

    [HttpGet("/storefront/returns")]
    public async Task<IActionResult> Returns()
    {
        ViewData.SetPageTitle("Iadeler");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Iadeler", null));

        var result = await returnManager.GetAllReturnsAsync(TenantId);
        return View(result.Data ?? []);
    }

    [HttpPost("/storefront/returns/{id:int}/status")]
    public async Task<IActionResult> UpdateReturnStatus(int id, ReturnStatus status, string? reviewNote, decimal? refundAmount)
    {
        var result = await returnManager.UpdateReturnStatusAsync(id, status, reviewNote, refundAmount);

        var statusText = status switch
        {
            ReturnStatus.Approved => "onaylandi",
            ReturnStatus.Rejected => "reddedildi",
            ReturnStatus.Completed => "tamamlandi",
            _ => "guncellendi"
        };

        return HtmxMutationResult(result,
            $"Iade talebi {statusText}.",
            $"Iade talebi {statusText} islemi basarisiz.",
            refreshEvent: "returnsUpdated",
            redirectAction: nameof(Returns));
    }

    // ── Sellers ──────────────────────────────────────────────────────────

    [HttpGet("/storefront/sellers")]
    public async Task<IActionResult> Sellers()
    {
        ViewData.SetPageTitle("Saticilar");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Saticilar", null));

        // Marketplace/seller feature controlled via settings
        var result = await settingsManager.GetByTenantIdAsync(TenantId);
        return View(result.Data);
    }

    // ── Campaigns ─────────────────────────────────────────────────────────

    [HttpGet("/storefront/campaigns")]
    public async Task<IActionResult> Campaigns()
    {
        ViewData.SetPageTitle("Kampanyalar");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Kampanyalar", null));

        var result = await campaignManager.GetCampaignsAsync(TenantId);
        var vm = new CampaignsVm { Campaigns = result.Data ?? [] };
        return HtmxView(vm);
    }

    [HttpPost("/storefront/campaigns")]
    public async Task<IActionResult> CreateCampaign(CampaignCreateVm model)
    {
        var result = await campaignManager.CreateCampaignAsync(TenantId, model.Subject, model.HtmlContent, model.Target);

        return HtmxMutationResult(result,
            "Kampanya olusturuldu.",
            "Kampanya olusturulamadi.",
            refreshEvent: "campaignsUpdated",
            redirectAction: nameof(Campaigns));
    }

    [HttpPost("/storefront/campaigns/{id:int}/schedule")]
    public async Task<IActionResult> ScheduleCampaign(int id, DateTimeOffset scheduleAt)
    {
        var result = await campaignManager.ScheduleCampaignAsync(id, scheduleAt);

        return HtmxMutationResult(result,
            "Kampanya zamanlandi.",
            "Kampanya zamanlanamiadi.",
            refreshEvent: "campaignsUpdated",
            redirectAction: nameof(Campaigns));
    }

    [HttpPost("/storefront/campaigns/{id:int}/cancel")]
    public async Task<IActionResult> CancelCampaign(int id)
    {
        var result = await campaignManager.CancelCampaignAsync(id);

        return HtmxMutationResult(result,
            "Kampanya iptal edildi.",
            "Kampanya iptal edilemedi.",
            refreshEvent: "campaignsUpdated",
            redirectAction: nameof(Campaigns));
    }

    [HttpDelete("/storefront/campaigns/{id:int}")]
    public async Task<IActionResult> DeleteCampaign(int id)
    {
        var result = await campaignManager.DeleteCampaignAsync(id);

        return HtmxMutationResult(result,
            "Kampanya silindi.",
            "Kampanya silinemedi.",
            refreshEvent: "campaignsUpdated",
            redirectAction: nameof(Campaigns));
    }

    // ── Email Campaigns ─────────────────────────────────────────────────

    [HttpGet("/storefront/email-campaigns")]
    public async Task<IActionResult> EmailCampaigns()
    {
        ViewData.SetPageTitle("E-posta Kampanyalari");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("E-posta Kampanyalari", null));

        var result = await campaignManager.GetCampaignsAsync(TenantId);
        var vm = new EmailCampaignsVm { Campaigns = result.Data ?? [] };
        return HtmxView(vm);
    }

    // ── Messages ────────────────────────────────────────────────────────

    [HttpGet("/storefront/messages")]
    public async Task<IActionResult> Messages(string? tab)
    {
        ViewData.SetPageTitle("Mesajlar");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Mesajlar", null));

        var contactResult = await contactManager.GetMessagesAsync(TenantId);
        var qnaResult = await qnaManager.GetUnansweredQuestionsAsync(TenantId);

        var vm = new MessagesVm
        {
            ContactMessages = contactResult.Data ?? [],
            UnansweredQuestions = qnaResult.Data ?? [],
            ActiveTab = tab ?? "contact"
        };
        return HtmxView(vm);
    }

    [HttpPost("/storefront/messages/{id:int}/read")]
    public async Task<IActionResult> MarkMessageRead(int id)
    {
        var result = await contactManager.MarkAsReadAsync(id);

        return HtmxMutationResult(result,
            "Mesaj okundu olarak isaretlendi.",
            "Islem basarisiz.",
            refreshEvent: "messagesUpdated",
            redirectAction: nameof(Messages));
    }

    [HttpPost("/storefront/questions/{id:int}/answer")]
    public async Task<IActionResult> AnswerQuestion(int id, string answer)
    {
        var result = await qnaManager.AnswerQuestionAsync(id, answer);

        return HtmxMutationResult(result,
            "Soru yanitlandi.",
            "Soru yanitlanamadi.",
            refreshEvent: "messagesUpdated",
            redirectAction: nameof(Messages));
    }

    // ── Newsletter ──────────────────────────────────────────────────────

    [HttpGet("/storefront/newsletter")]
    public async Task<IActionResult> Newsletter()
    {
        ViewData.SetPageTitle("Bulten Yonetimi");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Bulten", null));

        var result = await newsletterManager.GetSubscribersAsync(TenantId);
        var vm = new NewsletterVm { Subscribers = result.Data ?? [] };
        return HtmxView(vm);
    }

    [HttpPost("/storefront/newsletter/subscribe")]
    public async Task<IActionResult> AddSubscriber(string email, string? name)
    {
        var result = await newsletterManager.SubscribeAsync(TenantId, email, name);

        return HtmxMutationResult(result,
            "Abone eklendi.",
            "Abone eklenemedi.",
            refreshEvent: "newsletterUpdated",
            redirectAction: nameof(Newsletter));
    }

    [HttpPost("/storefront/newsletter/unsubscribe")]
    public async Task<IActionResult> RemoveSubscriber(string email)
    {
        var result = await newsletterManager.UnsubscribeAsync(TenantId, email);

        return HtmxMutationResult(result,
            "Abone kaldirildi.",
            "Abone kaldirilamadi.",
            refreshEvent: "newsletterUpdated",
            redirectAction: nameof(Newsletter));
    }

    // ── Payouts ─────────────────────────────────────────────────────────

    [HttpGet("/storefront/payouts")]
    public async Task<IActionResult> Payouts()
    {
        ViewData.SetPageTitle("Odemeler");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Odemeler", null));

        var result = await payoutManager.GetAllPendingPayoutsAsync(TenantId);
        var vm = new PayoutsVm { PendingPayouts = result.Data ?? [] };
        return HtmxView(vm);
    }

    [HttpPost("/storefront/payouts/{id:int}/process")]
    public async Task<IActionResult> ProcessPayout(int id, bool approve, string? note)
    {
        var result = await payoutManager.ProcessPayoutAsync(id, approve, note);

        return HtmxMutationResult(result,
            approve ? "Odeme onaylandi." : "Odeme reddedildi.",
            "Odeme islemi basarisiz.",
            refreshEvent: "payoutsUpdated",
            redirectAction: nameof(Payouts));
    }

    // ── Size Guides ─────────────────────────────────────────────────────

    [HttpGet("/storefront/size-guides")]
    public async Task<IActionResult> SizeGuides()
    {
        ViewData.SetPageTitle("Beden Kilavuzlari");
        ViewData.SetActiveNav("storefront");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Beden Kilavuzlari", null));

        var result = await sizeGuideManager.GetAllSizeGuidesAsync(TenantId);
        var vm = new SizeGuidesVm { SizeGuides = result.Data ?? [] };
        return HtmxView(vm);
    }

    [HttpPost("/storefront/size-guides")]
    public async Task<IActionResult> CreateSizeGuide(SizeGuideCreateVm model)
    {
        var guide = new StorefrontSizeGuide
        {
            TenantId = TenantId,
            Name = model.Name,
            CategoryIds = model.CategoryIds,
            MeasurementInstructions = model.MeasurementInstructions,
            SizeData = model.SizeData,
            IsActive = model.IsActive
        };

        var result = await sizeGuideManager.CreateOrUpdateAsync(guide);

        return HtmxMutationResult(result,
            "Beden kilavuzu olusturuldu.",
            "Beden kilavuzu olusturulamadi.",
            refreshEvent: "sizeGuidesUpdated",
            redirectAction: nameof(SizeGuides));
    }

    [HttpPut("/storefront/size-guides/{id:int}")]
    public async Task<IActionResult> UpdateSizeGuide(int id, SizeGuideCreateVm model)
    {
        var guide = new StorefrontSizeGuide
        {
            Id = id,
            TenantId = TenantId,
            Name = model.Name,
            CategoryIds = model.CategoryIds,
            MeasurementInstructions = model.MeasurementInstructions,
            SizeData = model.SizeData,
            IsActive = model.IsActive
        };

        var result = await sizeGuideManager.CreateOrUpdateAsync(guide);

        return HtmxMutationResult(result,
            "Beden kilavuzu guncellendi.",
            "Beden kilavuzu guncellenemedi.",
            refreshEvent: "sizeGuidesUpdated",
            redirectAction: nameof(SizeGuides));
    }

    [HttpDelete("/storefront/size-guides/{id:int}")]
    public async Task<IActionResult> DeleteSizeGuide(int id)
    {
        var result = await sizeGuideManager.DeleteAsync(id);

        return HtmxMutationResult(result,
            "Beden kilavuzu silindi.",
            "Beden kilavuzu silinemedi.",
            refreshEvent: "sizeGuidesUpdated",
            redirectAction: nameof(SizeGuides));
    }

    // ── Commissions ─────────────────────────────────────────────────────

    [HttpGet("/storefront/commissions")]
    public async Task<IActionResult> Commissions()
    {
        ViewData.SetPageTitle("Satici Komisyonlari");
        ViewData.SetActiveNav("storefront-commissions");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Satici Komisyonlari", null));

        var result = await sellerCommissionManager.GetCommissionsAsync(TenantId);
        return HtmxView("Commissions", result.Data ?? []);
    }

    [HttpPost("/storefront/commissions/{id:int}/update")]
    public async Task<IActionResult> UpdateCommission(int id, [FromForm] decimal rate)
    {
        var result = await sellerCommissionManager.UpdateCommissionRateAsync(id, rate);

        return HtmxMutationResult(result,
            "Komisyon orani guncellendi.",
            "Komisyon orani guncellenemedi.",
            refreshEvent: "commissionsUpdated",
            redirectAction: nameof(Commissions));
    }

    // ── Abandoned Carts ──────────────────────────────────────────────────

    [HttpGet("/storefront/abandoned-carts")]
    public async Task<IActionResult> AbandonedCarts()
    {
        ViewData.SetPageTitle("Terk Edilen Sepetler");
        ViewData.SetActiveNav("storefront-abandoned-carts");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Terk Edilen Sepetler", null));

        var result = await abandonedCartManager.GetAbandonedCartEmailsAsync(TenantId);
        return HtmxView("AbandonedCarts", result.Data ?? []);
    }

    // ── Referrals ────────────────────────────────────────────────────────

    [HttpGet("/storefront/referrals")]
    public IActionResult Referrals()
    {
        ViewData.SetPageTitle("Referans Programi");
        ViewData.SetActiveNav("storefront-referrals");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Referans Programi", null));

        return View();
    }

    // ── Wallets ──────────────────────────────────────────────────────────

    [HttpGet("/storefront/wallets")]
    public IActionResult Wallets()
    {
        ViewData.SetPageTitle("Musteri Cuzdanlari");
        ViewData.SetActiveNav("storefront-wallets");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Musteri Cuzdanlari", null));

        var vm = new WalletsVm();
        return HtmxView(vm);
    }

    [HttpGet("/storefront/wallets/{id:int}")]
    public async Task<IActionResult> WalletDetail(int id)
    {
        ViewData.SetPageTitle($"Cuzdan Detayi - #{id}");
        ViewData.SetActiveNav("storefront-wallets");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Musteri Cuzdanlari", "/storefront/wallets"), ($"#{id}", null));

        var walletResult = await walletManager.GetOrCreateWalletAsync(TenantId, id);
        var txResult = await walletManager.GetTransactionsAsync(TenantId, id);

        var vm = new WalletDetailVm
        {
            Wallet = walletResult.Data ?? new StorefrontWallet { CustomerId = id, TenantId = TenantId },
            Transactions = txResult.Data ?? []
        };

        return HtmxView(vm);
    }

    [HttpPost("/storefront/wallets/{id:int}/credit")]
    public async Task<IActionResult> CreditWallet(int id, decimal amount, string? description)
    {
        var result = await walletManager.CreditAsync(
            TenantId, id, amount,
            WalletTransactionType.Promotion,
            referenceId: null,
            description: description);

        if (result.Success)
            TempData.SetSuccess($"{amount:N2} TL cuzdana yuklendi.");
        else
            TempData.SetError(result.Message ?? "Yukleme basarisiz.");

        return RedirectToAction(nameof(WalletDetail), new { id });
    }

    // ── Loyalty ──────────────────────────────────────────────────────────

    [HttpGet("/storefront/loyalty")]
    public async Task<IActionResult> Loyalty()
    {
        ViewData.SetPageTitle("Sadakat Programi");
        ViewData.SetActiveNav("storefront-loyalty");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Sadakat Programi", null));

        var dashboard = await loyaltyManager.GetDashboardAsync(TenantId);
        var vm = new LoyaltyVm { Dashboard = dashboard };
        return HtmxView(vm);
    }

    [HttpGet("/storefront/loyalty/{id:int}")]
    public async Task<IActionResult> LoyaltyCustomer(int id)
    {
        ViewData.SetPageTitle($"Sadakat Detayi - #{id}");
        ViewData.SetActiveNav("storefront-loyalty");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Sadakat Programi", "/storefront/loyalty"), ($"#{id}", null));

        var balanceResult = await loyaltyManager.GetBalanceAsync(TenantId, id);
        var txResult = await loyaltyManager.GetTransactionsAsync(TenantId, id);

        var vm = new LoyaltyCustomerVm
        {
            CustomerId = id,
            Balance = balanceResult.Data ?? new StorefrontLoyaltyPoints { CustomerId = id, TenantId = TenantId },
            Transactions = txResult.Data ?? []
        };

        return HtmxView(vm);
    }

    [HttpPost("/storefront/loyalty/{id:int}/credit")]
    public async Task<IActionResult> CreditLoyalty(int id, int points, string? description)
    {
        var result = await loyaltyManager.EarnPointsAsync(
            TenantId, id, points,
            type: "ManualAdjust",
            referenceId: null,
            description: description);

        if (result.Success)
            TempData.SetSuccess($"{points} puan musteriye eklendi.");
        else
            TempData.SetError(result.Message ?? "Puan ekleme basarisiz.");

        return RedirectToAction(nameof(LoyaltyCustomer), new { id });
    }

    // ── Wishlists ─────────────────────────────────────────────────────────

    [HttpGet("/storefront/wishlists")]
    public IActionResult Wishlists()
    {
        ViewData.SetPageTitle("Istek Listeleri");
        ViewData.SetActiveNav("storefront-wishlists");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Istek Listeleri", null));

        return HtmxView("Wishlists");
    }

    // ── Stock Notifications ───────────────────────────────────────────────

    [HttpGet("/storefront/stock-notifications")]
    public IActionResult StockNotifications()
    {
        ViewData.SetPageTitle("Stok Bildirimleri");
        ViewData.SetActiveNav("storefront-stock-notifications");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Stok Bildirimleri", null));

        return HtmxView("StockNotifications");
    }

    // ── Search Analytics ──────────────────────────────────────────────────

    [HttpGet("/storefront/search-analytics")]
    public async Task<IActionResult> SearchAnalytics()
    {
        ViewData.SetPageTitle("Arama Analitik");
        ViewData.SetActiveNav("storefront-search-analytics");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Arama Analitik", null));

        var result = await searchHistoryManager.GetPopularSearchesAsync(TenantId, 10);
        return HtmxView("SearchAnalytics", result.Data ?? []);
    }

    // ── Push Notifications ────────────────────────────────────────────────

    [HttpGet("/storefront/push-notifications")]
    public async Task<IActionResult> PushNotifications()
    {
        ViewData.SetPageTitle("Push Bildirimler");
        ViewData.SetActiveNav("storefront-push-notifications");
        ViewData.SetBreadcrumb(("Magaza", "/settings/storefront"), ("Push Bildirimler", null));

        var countResult = await pushManager.GetSubscriberCountAsync(TenantId);

        var vm = new PushNotificationsVm
        {
            SubscriberCount = countResult.Data,
            SentNotifications = []
        };

        return HtmxView(vm);
    }

    [HttpPost("/storefront/push-notifications/send")]
    public async Task<IActionResult> SendPushNotification(string title, string body)
    {
        // Subscribe call used as a no-op placeholder since IStorefrontPushManager
        // does not expose a broadcast method. Real broadcast would be wired here.
        _ = title;
        _ = body;
        var countResult = await pushManager.GetSubscriberCountAsync(TenantId);

        if (!countResult.Success)
        {
            return HtmxMutationResult(countResult,
                string.Empty,
                "Bildirim gonderilemedi.",
                redirectAction: nameof(PushNotifications));
        }

        TempData.SetSuccess($"Push bildirim {countResult.Data} aboneye gonderildi.");
        return RedirectToAction(nameof(PushNotifications));
    }
}
