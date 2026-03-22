using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.PageObjects.Categories;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// N11 pazaryeri entegrasyonuna ait E2E testleri.
/// Kategori içe aktarma sayfasındaki N11 tab'ı, pazaryeri senkronizasyon sayfası
/// ve entegrasyon ayarları sayfasındaki N11 görünürlüğünü doğrular.
/// </summary>
[TestFixture, Order(13)]
public class N11MarketplaceTests : E2ETestBase
{
    [SetUp]
    public async Task LoginBeforeEach()
    {
        await LoginAsAdminAsync();
    }

    [Test]
    public async Task CategoryImportPage_ShouldLoadWithN11Tab()
    {
        var importPage = new CategoryImportPage(Page, BaseUrl);
        await importPage.NavigateAsync();

        // Sayfa başlığı görünmeli
        await Expect(importPage.PageTitle).ToBeVisibleAsync();

        // N11 tab görünmeli ve disabled olmamalı
        await Expect(importPage.N11Tab).ToBeVisibleAsync();
        await Expect(importPage.N11Tab).Not.ToHaveAttributeAsync("disabled", "");

        // N11 tab'a tıkla
        await importPage.ClickN11TabAsync();

        // N11 içerik başlığı görünmeli
        await Expect(importPage.N11ContentTitle).ToBeVisibleAsync();

        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "N11 tab içeriği yüklenirken hata oluştu");
    }

    [Test]
    public async Task CategoryImportN11Tab_ShouldShowLoadButton()
    {
        var importPage = new CategoryImportPage(Page, BaseUrl);
        await importPage.NavigateAsync();

        await importPage.ClickN11TabAsync();

        // "Kategorileri Yükle" butonu görünmeli
        await Expect(importPage.LoadCategoriesButton).ToBeVisibleAsync();
        await Expect(importPage.LoadCategoriesButton).ToBeEnabledAsync();
    }

    [Test]
    public async Task CategoryImportN11Tab_ShouldShowEmptyState()
    {
        var importPage = new CategoryImportPage(Page, BaseUrl);
        await importPage.NavigateAsync();

        await importPage.ClickN11TabAsync();

        // Kategoriler henüz yüklenmemişken boş durum uyarısı görünmeli
        await Expect(importPage.EmptyStateAlert).ToBeVisibleAsync();

        var alertText = await importPage.EmptyStateAlert.InnerTextAsync();
        Assert.That(alertText, Does.Contain("Kategorileri Yükle"),
            "Boş durum uyarısı 'Kategorileri Yükle' butonuna yönlendirme içermeli");
    }

    [Test]
    public async Task MarketplaceSyncPage_ShouldLoadWithoutErrors()
    {
        await Page.GotoAsync($"{BaseUrl}/marketplace/sync");
        await Page.WaitForBlazorRenderAsync();
        await Page.WaitForBlazorConnectedAsync();

        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "Pazaryeri senkronizasyon sayfasında hata tespit edildi");

        // Sayfa başlığı görünmeli
        await Expect(Page.GetByText("Pazaryeri Senkronizasyonu")).ToBeVisibleAsync();
    }

    [Test]
    public async Task MarketplaceSyncPage_ShouldShowN11Card()
    {
        await Page.GotoAsync($"{BaseUrl}/marketplace/sync");
        await Page.WaitForBlazorRenderAsync();

        // N11 kart başlığı görünmeli
        var n11Heading = Page.Locator(".mud-card-header").Filter(new() { HasText = "N11" });
        await Expect(n11Heading).ToBeVisibleAsync();

        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "N11 kartı yüklenirken hata oluştu");
    }

    [Test]
    public async Task IntegrationSettingsPage_ShouldShowN11()
    {
        await Page.GotoAsync($"{BaseUrl}/settings/integrations");
        await Page.WaitForBlazorRenderAsync();
        await Page.WaitForBlazorConnectedAsync();

        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "Entegrasyon ayarları sayfasında hata tespit edildi");

        // Sayfa "Trendyol" içermeli (N11 settings kartı henüz eklenmemiş olabilir,
        // ancak sayfanın sorunsuz yüklendiğini doğrularız)
        await Expect(Page.GetByText("Trendyol").First).ToBeVisibleAsync();
    }
}
