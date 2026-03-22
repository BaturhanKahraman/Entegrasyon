using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.Tests.P0_Smoke;

/// <summary>
/// TÜM route'ları ziyaret eder ve hiçbirinde hata olmadığını doğrular.
/// Bu tek test bile "bir şey bozulmuş mu?" sorusunu büyük ölçüde yanıtlar.
/// Her sayfada ErrorBoundary veya Blazor hata UI'ı aranır.
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
        "/attributes",
        "/brands",
        "/sales",
        "/orders",
        "/customers",
        "/invoices",
        "/marketplace/sync",
        "/marketplace/sync/categories",
        "/marketplace/sync/attributes",
        "/marketplace/sync/brands",
        "/marketplace/matching",
        "/marketplace/orders",
        "/reports/sales",
        "/reports/inventory",
        "/reports/marketplace",
        "/users",
        "/roles",
        "/settings/general",
        "/settings/notifications",
        "/settings/integrations",
        "/settings/printing",
        "/profile",
        "/notifications"
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
                await Page.WaitForBlazorRenderAsync();
                await Page.WaitForBlazorConnectedAsync();

                var hasNoError = await Page.HasNoErrorAsync();
                if (!hasNoError)
                    failedRoutes.Add($"{route} — ErrorBoundary veya Blazor hatası görüldü");

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
        var mainContent = Page.Locator(".mud-main-content");
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
        var drawer = Page.Locator(".mud-drawer");
        await Expect(drawer).ToBeVisibleAsync();

        // Temel menü grupları görünmeli
        await Expect(Page.GetByText("Yönetim")).ToBeVisibleAsync();
    }
}
