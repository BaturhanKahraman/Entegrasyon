using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.TestData;

namespace Entegrasyon.E2E.Tests.P3_Notifications;

/// <summary>
/// Bildirim sistemi E2E testleri.
///
/// Kapsanan akışlar:
///   1. ProductAdded_BadgeUpdatesInOtherUserTab — A kullanıcısı ürün ekler → B kullanıcısının rozeti SSE ile güncellenir.
///   2. ClickNotification_NavigatesToActionUrl   — Çan dropdown'ından bildirime tıkla → ActionUrl'e git.
///   3. MarkAsRead_SyncsAcrossTabs              — Aynı oturumda iki sekme: A sekmesinde okundu → B sekmesinin rozeti SSE ile azalır.
///
/// NOT: Test 1 (çok kullanıcılı senaryo) yalnızca tek admin kullanıcısı mevcut olduğu için
///      şu an Assert.Inconclusive ile atlanır. CI'da ikinci kullanıcı eklendikten sonra
///      tam akışa geçiş için aşağıdaki TODO talimatları izlenir.
/// </summary>
[TestFixture, Order(200)]
public class NotificationE2ETests : E2ETestBase
{
    [SetUp]
    public async Task SetUp()
    {
        await LoginAsAdminAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 1 — Çok kullanıcılı SSE rozet güncellemesi
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Kullanıcı A ürün eklediğinde, Kullanıcı B'nin bell rozeti SSE üzerinden güncellenir.
    ///
    /// Şu an tek test kullanıcısı mevcut (admin). İkinci kullanıcı DB'ye seed edildiğinde:
    ///   1. TestUsers'a ikinci kullanıcı sabiti ekle.
    ///   2. TestDataSeeder.SeedAsync() içinde kullanıcıyı oluştur.
    ///   3. Aşağıdaki Assert.Inconclusive bloğunu kaldır ve tam akışa geç.
    /// </summary>
    [Test, Order(1)]
    public async Task ProductAdded_BadgeUpdatesInOtherUserTab()
    {
        // TODO: İkinci test kullanıcısı (observer) DB'ye seed edilene kadar bu test atlanır.
        // Seeding yapılırken TestDataSeeder'a aşağıdaki satır eklenir:
        //   await SeedObserverUserAsync(conn);  // username: observer, password: 123456789
        Assert.Inconclusive(
            "İkinci test kullanıcısı (observer) henüz DB'ye seed edilmedi. " +
            "TestDataSeeder'a SeedObserverUserAsync() eklendikten sonra bu testi etkinleştir.");

        // ── Tam akış (ikinci kullanıcı seed edilince aktif olacak) ──────────
        //
        // await using var browserA = await Playwright.Chromium.LaunchAsync(new() { Headless = true });
        // await using var browserB = await Playwright.Chromium.LaunchAsync(new() { Headless = true });
        //
        // // Kullanıcı A — işlem yapan
        // var ctxA = await browserA.NewContextAsync(ContextOptions());
        // var pageA = await ctxA.NewPageAsync();
        // await AuthHelper.LoginAsync(pageA, BaseUrl, TestUsers.AdminUsername, TestUsers.AdminPassword);
        //
        // // Kullanıcı B — gözlemci (ürün ekleme yetkisi olan başka kullanıcı)
        // var ctxB = await browserB.NewContextAsync(ContextOptions());
        // var pageB = await ctxB.NewPageAsync();
        // await AuthHelper.LoginAsync(pageB, BaseUrl, "observer", "123456789");
        //
        // await pageB.GotoAsync($"{BaseUrl}/");
        // await pageB.WaitForLoadStateAsync(LoadState.NetworkIdle);
        //
        // // SSE bağlantısının kurulması için kısa süre bekle
        // await pageB.WaitForTimeoutAsync(1500);
        //
        // // Başlangıç rozet durumunu kaydet
        // var badge = pageB.Locator("[data-notification-badge]");
        // var initialBadgeHidden = await badge.EvaluateAsync<bool>("el => el.classList.contains('d-none')");
        // var initialCount = initialBadgeHidden ? 0 : int.Parse(await badge.TextContentAsync() ?? "0");
        //
        // // Kullanıcı A ürün ekler
        // await pageA.GotoAsync($"{BaseUrl}/products/add");
        // await pageA.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // await pageA.FillAsync("input[name='Title']", "E2E Bildirim Test " + DateTime.Now.Ticks);
        // // ... kategori ve marka seç (ProductWizardTests kalıbı)
        // await pageA.Locator("[data-wizard-step='1'] button[type='submit']").ClickAndWaitForHtmxAsync(pageA);
        //
        // // Kullanıcı B'nin rozetinin güncellenmesini bekle (SSE ≤10 sn içinde)
        // await Expect(badge).Not.ToHaveClassAsync(new Regex(@"\bd-none\b"), new() { Timeout = 10000 });
        // var newCount = int.Parse(await badge.TextContentAsync() ?? "0");
        // Assert.That(newCount, Is.GreaterThan(initialCount), "SSE üzerinden rozet artmalı");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 2 — Çan dropdown bildirim navigasyonu
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Çan dropdown'ındaki bir bildirime tıklandığında ActionUrl'e gidildiğini doğrular.
    ///
    /// Ön koşul: Test kullanıcısının en az bir okunmamış bildirimi olmalı.
    /// Bildirim yoksa Assert.Inconclusive ile atlanır (setup sorunu, test hatası değil).
    /// </summary>
    [Test, Order(2)]
    public async Task ClickNotification_NavigatesToActionUrl()
    {
        await Page.GotoAsync($"{BaseUrl}/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Çan butonunu kontrol et
        var bellButton = Page.Locator("[data-notification-bell]");
        await Expect(bellButton).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Rozette okunmamış bildirim var mı?
        var badge = Page.Locator("[data-notification-badge]");
        var badgeHidden = await badge.EvaluateAsync<bool>("el => el.classList.contains('d-none')");
        if (badgeHidden)
        {
            Assert.Inconclusive(
                "Test kullanıcısının okunmamış bildirimi yok — navigasyon testi atlanıyor. " +
                "Bir önceki test çalıştırıldığında bildirim oluşturulmuş olmalı.");
            return;
        }

        // Dropdown'ı aç
        await bellButton.ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        // Dropdown listesindeki ilk bildirim öğesini bul
        var dropdownList = Page.Locator("[data-notification-dropdown-list]");
        await Expect(dropdownList).ToBeVisibleAsync(new() { Timeout = 5000 });

        var firstItem = dropdownList.Locator("[data-notification-id]").First;
        var itemCount = await dropdownList.Locator("[data-notification-id]").CountAsync();
        if (itemCount == 0)
        {
            Assert.Inconclusive("Dropdown'da bildirim öğesi bulunamadı.");
            return;
        }

        // href attr'sini al — navigasyon hedefini önceden kaydet
        var href = await firstItem.GetAttributeAsync("href") ?? "/notifications";

        // Tıkla
        await firstItem.ClickAsync();

        // URL'in beklenen hedefe değişmesini bekle
        // Bildirim ActionUrl'e ya da /notifications'a gider
        await Page.WaitForURLAsync(new Regex(@"/(products|notifications|categories|brands)"), new() { Timeout = 8000 });

        TestContext.Progress.WriteLine($"[E2E] Notification click navigated to: {Page.Url} (expected href: {href})");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test 3 — Sekmeler arası okundu senkronizasyonu
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Aynı browser context'inde (aynı auth cookie) iki sekme açılır.
    /// Sekme A'da bir bildirim okundu işaretlenir → Sekme B'nin rozeti SSE ile azalır.
    ///
    /// Aynı IBrowserContext kullanıldığı için auth cookie paylaşılır.
    /// </summary>
    [Test, Order(3)]
    public async Task MarkAsRead_SyncsAcrossTabs()
    {
        // Sekme A: bildirimler sayfası (SetUp'ta login yapıldı, Page Sekme A)
        await Page.GotoAsync($"{BaseUrl}/notifications");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Okunmamış bildirim var mı kontrol et
        var unreadItems = Page.Locator(".list-group-item button[hx-post*='/read']");
        var unreadCount = await unreadItems.CountAsync();

        if (unreadCount == 0)
        {
            Assert.Inconclusive(
                "Test kullanıcısının okunmamış bildirimi yok — sekme senkronizasyon testi atlanıyor. " +
                "Önce bir ürün eklenerek bildirim oluşturulmalı.");
            return;
        }

        // Sekme B: aynı context'te (aynı auth cookie) yeni sayfa
        var pageB = await Context.NewPageAsync();
        await pageB.GotoAsync($"{BaseUrl}/");
        await pageB.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // SSE bağlantısının kurulması için kısa bekleme
        await pageB.WaitForTimeoutAsync(1500);

        // Sekme B'deki başlangıç rozet sayısını kaydet
        var badgeB = pageB.Locator("[data-notification-badge]");
        await Expect(badgeB).ToBeVisibleAsync(new() { Timeout = 5000 });

        var initialBadgeHiddenB = await badgeB.EvaluateAsync<bool>("el => el.classList.contains('d-none')");
        if (initialBadgeHiddenB)
        {
            Assert.Inconclusive(
                "Sekme B'de rozet görünmüyor — okunmamış bildirim yoktur, senkronizasyon testi anlamsız.");
            await pageB.CloseAsync();
            return;
        }

        var initialCountText = await badgeB.TextContentAsync() ?? "0";
        var initialCountB = initialCountText.Trim() == "99+" ? 99 : int.Parse(initialCountText.Trim() == "" ? "0" : initialCountText.Trim());

        // Sekme A'da ilk okunmamış bildirimi okundu işaretle
        var firstUnread = unreadItems.First;
        await firstUnread.ClickAndWaitForHtmxAsync(Page, 10000);

        // SSE üzerinden Sekme B'nin rozeti azalmalı (10 sn timeout)
        // Beklenen: initialCountB - 1
        var expectedCount = initialCountB - 1;

        if (expectedCount == 0)
        {
            // Rozet gizlenmeli
            await Expect(badgeB).ToHaveClassAsync(new Regex(@"\bd-none\b"), new() { Timeout = 10000 });
        }
        else
        {
            await Expect(badgeB).ToHaveTextAsync(
                new Regex($@"^\s*{expectedCount}\s*$"),
                new() { Timeout = 10000 });
        }

        TestContext.Progress.WriteLine($"[E2E] Tab sync: badge went from {initialCountB} to {expectedCount}");

        await pageB.CloseAsync();
    }
}
