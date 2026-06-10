using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.PageObjects;
using Entegrasyon.E2E.TestData;

namespace Entegrasyon.E2E.Tests.P0_Smoke;

/// <summary>
/// Login akışının doğru çalıştığını test eder.
/// Hem başarılı hem başarısız senaryolar.
/// </summary>
[TestFixture, Order(1)]
public class LoginTests : E2ETestBase
{
    [Test]
    public async Task ValidLogin_RedirectsToDashboard()
    {
        var loginPage = new LoginPage(Page, BaseUrl);
        await loginPage.NavigateAsync();

        await loginPage.LoginAndWaitForDashboardAsync(
            TestUsers.AdminUsername,
            TestUsers.AdminPassword);

        // Dashboard'da olmalıyız
        await Expect(Page).ToHaveURLAsync($"{BaseUrl}/");

        // Ana içerik yüklenmeli
        var mainContent = Page.Locator(".page-body");
        await Expect(mainContent).ToBeVisibleAsync();
    }

    [Test]
    public async Task InvalidLogin_ShowsErrorAlert()
    {
        var loginPage = new LoginPage(Page, BaseUrl);
        await loginPage.NavigateAsync();

        await loginPage.LoginAsync("yanlis_kullanici", "yanlis_Şifre");

        // .alert-danger hata mesajı gösterilmeli (PRG: sayfa yeniden gösterilir)
        await Expect(loginPage.ErrorAlert).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task EmptyCredentials_ShowsValidationError()
    {
        var loginPage = new LoginPage(Page, BaseUrl);
        await loginPage.NavigateAsync();

        // Boş form ile submit et
        await loginPage.LoginButton.ClickAsync();

        // Validation hatası gösterilmeli (.alert-danger veya .text-danger)
        var hasError = await Page.Locator(".alert-danger, .text-danger").CountAsync() > 0;
        Assert.That(hasError, Is.True, "Boş form submit edildiğinde validation hatası görülmeli");

        // Login sayfasında kalmalıyız
        await Expect(Page).ToHaveURLAsync(new Regex(@"/auth/login"));
    }
}
