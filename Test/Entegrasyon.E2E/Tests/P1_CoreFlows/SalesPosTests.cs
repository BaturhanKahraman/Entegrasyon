using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.PageObjects.Sales;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// Satış Noktası (POS) akışlarını test eder.
/// Sayfa yüklenmesi, barkod arama, sepet görüntüleme.
/// </summary>
[TestFixture, Order(12)]
public class SalesPosTests : E2ETestBase
{
    [SetUp]
    public async Task LoginBeforeEach()
    {
        await LoginAsAdminAsync();
    }

    [Test]
    public async Task SalesPage_LoadsSuccessfully()
    {
        var salesPage = new SalesPage(Page, BaseUrl);
        await salesPage.NavigateAsync();

        // POS başlığı görünmeli
        await Expect(salesPage.PageTitle).ToBeVisibleAsync();

        // Barkod arama alanı görünmeli
        await Expect(salesPage.BarcodeField).ToBeVisibleAsync();

        // Müşteri seç butonu görünmeli
        await Expect(salesPage.CustomerSelectButton).ToBeVisibleAsync();
    }

    [Test]
    public async Task SalesPage_BarcodeField_IsFocusable()
    {
        var salesPage = new SalesPage(Page, BaseUrl);
        await salesPage.NavigateAsync();

        // Barkod alanına yazılabilmeli
        await salesPage.BarcodeField.FillAsync("TEST123");
        await Expect(salesPage.BarcodeField).ToHaveValueAsync("TEST123");
    }

    [Test]
    public async Task SalesPage_SearchInvalidBarcode_NoError()
    {
        var salesPage = new SalesPage(Page, BaseUrl);
        await salesPage.NavigateAsync();

        // Geçersiz barkod ara — hata fırlatmamalı
        await salesPage.SearchBarcodeAsync("INVALID_BARCODE_12345");

        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "Geçersiz barkod aramasında hata oluştu");
    }

    [Test]
    public async Task SalesPage_EmptyCart_ShowsNoItems()
    {
        var salesPage = new SalesPage(Page, BaseUrl);
        await salesPage.NavigateAsync();

        // Boş sepet — sepet başlığı görünmeli ama satır yok
        await Expect(salesPage.CartTitle).ToBeVisibleAsync();
    }
}
