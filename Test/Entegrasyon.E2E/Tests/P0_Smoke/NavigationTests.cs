using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.Tests.P0_Smoke;

/// <summary>
/// TÜM route'ları ziyaret eder ve hiçbirinde hata olmadığını doğrular.
/// Bu tek test bile "bir şey bozulmuş mu?" sorusunu büyük ölçüde yanıtlar.
/// Her sayfada hata UI'ı veya 500 sayfası aranır.
/// </summary>
[TestFixture, Order(2)]
public class NavigationTests : E2ETestBase
{
    /// <summary>
    /// Tüm route'lar — her yeni sayfa eklendiğinde buraya da eklenmeli.
    /// </summary>
    private static readonly string[] AllRoutes =
    [
        "/",
        "/products",
        "/products/add",
        "/categories",
        "/categories/import",
        "/attributes",
        "/brands",
        "/pos",
        "/orders",
        "/customers",
        "/invoices",
        // Satislar — daha once 500 veren rota (P0 kritik)
        "/sales",
        "/marketplace/sync",
        "/marketplace/sync/categories",
        "/marketplace/sync/attributes",
        "/marketplace/sync/brands",
        "/marketplace/matching",
        "/marketplace/orders",
        "/reports/sales",
        "/reports/inventory",
        "/reports/marketplace",
        "/reports/customers",
        "/reports/returns",
        "/reports/tax",
        "/reports/category-sales",
        "/reports/shipping",
        "/users",
        "/roles",
        "/settings/general",
        "/settings/notifications",
        "/settings/integrations",
        "/settings/printing",
        "/settings/tax",
        // Odeme yontemleri — kritik ayar sayfasi
        "/settings/payment-methods",
        "/settings/shipping",
        "/settings/webhooks",
        "/settings/api-keys",
        "/profile",
        "/profile/change-password",
        "/profile/activity",
        "/notifications",
        "/logs",
        "/stock/movements",
        "/discounts",
        "/discounts/create",
        "/gift-cards",
        "/gift-cards/create",
        "/storefront/commissions",
        "/storefront/abandoned-carts",
        "/storefront/wallets",
        "/storefront/wishlists",
        "/storefront/stock-notifications",
        "/storefront/search-analytics",
        "/storefront/push-notifications",
        "/storefront/referrals",
        "/shipping/companies",
        "/returns",
        "/picking",
        "/integrations/health",
        "/customers/1/dashboard",
        "/pricing",
        "/pricing/rules",
        "/loyalty",
        "/help",
        // Sube ofisleri — daha once 500 veren rota ailesi (P0 kritik)
        // Id=1 TestDataSeeder tarafindan seed edilir (Merkez Sube)
        "/branch-offices",
        "/branch-offices/1"
    ];

    [SetUp]
    public async Task LoginBeforeTest()
    {
        await LoginAsAdminAsync();
    }

    [Test]
    public async Task AllRoutes_LoadWithoutCrash()
    {
        var failedRoutes = new List<string>();

        foreach (var route in AllRoutes)
        {
            await Page.GotoAsync($"{BaseUrl}{route}");

            try
            {
                await Page.WaitForHtmxSettleAsync();

                var hasNoError = await Page.HasNoErrorAsync();
                if (!hasNoError)
                    failedRoutes.Add($"{route} — Hata sayfası tespit edildi");

                // Auth redirect kontrolü — session düştüyse login'e yönlenmiş olabilir
                if (Page.Url.Contains("/auth/login"))
                {
                    failedRoutes.Add($"{route} — Oturum düşmüş, login sayfasına yönlendirildi");
                    await LoginAsAdminAsync();
                }
            }
            catch (Exception ex)
            {
                failedRoutes.Add($"{route} — {ex.Message}");
            }
        }

        Assert.That(failedRoutes, Is.Empty,
            $"Hatalı route'lar:\n{string.Join('\n', failedRoutes)}");
    }

    [Test]
    public async Task Dashboard_LoadsSuccessfully()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Dashboard içeriği yüklenmeli
        var mainContent = Page.Locator(".page-body");
        await Expect(mainContent).ToBeVisibleAsync();

        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "Dashboard'da hata tespit edildi");
    }

    [Test]
    public async Task NavMenu_IsVisible_AfterLogin()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Sol menü görünmeli
        var drawer = Page.Locator("aside.navbar-vertical");
        await Expect(drawer).ToBeVisibleAsync();

        // Temel menü linkleri görünmeli
        await Expect(Page.Locator("aside.navbar-vertical .nav-link").First).ToBeVisibleAsync();
    }
}
