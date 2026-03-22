using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.Tests.P0_Smoke;

/// <summary>
/// Uygulamanın ayakta olduğunu ve temel sayfa yüklemelerinin çalıştığını doğrular.
/// Tüm testlerden önce çalışır — başarısız olursa geri kalanı çalıştırmanın anlamı yoktur.
/// </summary>
[TestFixture, Order(0)]
public class AppStartupTests : E2ETestBase
{
    [Test]
    public async Task App_ReturnsLoginPage_WhenNotAuthenticated()
    {
        await Page.GotoAsync(BaseUrl);

        // Unauthenticated kullanıcı login'e yönlendirilmeli
        await Page.WaitForURLAsync($"**/auth/login**", new() { Timeout = 15000 });

        // Login formu görünmeli
        await Expect(Page.GetByLabel("Kullanıcı Adı")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Şifre")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Giriş Yap" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task App_BlazorSignalR_Connects()
    {
        await Page.GotoAsync($"{BaseUrl}/auth/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Blazor bağlantı hatası olmadığını doğrula
        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "Blazor SignalR bağlantı hatası tespit edildi");
    }
}
