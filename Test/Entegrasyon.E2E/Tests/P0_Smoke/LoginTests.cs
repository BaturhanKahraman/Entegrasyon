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
        var mainContent = Page.Locator(".mud-main-content");
        await Expect(mainContent).ToBeVisibleAsync();
    }

    [Test]
    public async Task InvalidLogin_ShowsErrorSnackbar()
    {
        var loginPage = new LoginPage(Page, BaseUrl);
        await loginPage.NavigateAsync();

        await loginPage.LoginAsync("yanlis_kullanici", "yanlis_sifre");

        // Snackbar hata mesajı gösterilmeli
        await Expect(loginPage.Snackbar).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task EmptyCredentials_ButtonIsDisabled()
    {
        var loginPage = new LoginPage(Page, BaseUrl);
        await loginPage.NavigateAsync();

        // Boş form → buton disabled olmalı (MudForm IsValid=false)
        await Expect(loginPage.LoginButton).ToBeDisabledAsync();

        // Login sayfasında kalmalıyız
        await Expect(Page).ToHaveURLAsync(new Regex(@"/auth/login"));
    }
}
