using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.PageObjects.Products;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// Ürün ekleme sırasında marka ve kategori seçim validasyonunu test eder.
/// MVC'de marka/kategori standart HTML select ile seçilir.
/// </summary>
[TestFixture, Order(11)]
public class BrandCategorySelectionTests : E2ETestBase
{
    private AddProductPage _addPage = null!;

    [SetUp]
    public async Task SetUp()
    {
        await LoginAsAdminAsync();
        _addPage = new AddProductPage(Page, BaseUrl);
        await _addPage.NavigateAsync();
    }

    [Test]
    public async Task CategorySelect_Required_ValidationBlocksEmptySubmit()
    {
        // Arrange — başlık ve stok kodu doldur ama kategori seçme
        var testId = Guid.NewGuid().ToString("N")[..8];
        await _addPage.TitleField.FillAsync($"E2E Ürün {testId}");
        await _addPage.StockCodeField.FillAsync($"E2E-{testId}");

        // İleri butonuna bas — kategori seçilmediği için validation hatası alınmalı
        await _addPage.ClickNextAsync();

        // Validation hatası görünmeli (.text-danger veya .alert-danger)
        var hasValidationError = await Page.Locator(".text-danger, .alert-danger, [data-valmsg-for]").CountAsync() > 0;
        Assert.That(hasValidationError, Is.True,
            "Kategori seçilmeden ileri gidilememeli — validation hatası bekleniyor");

        // Step 0'da kalmalıyız
        await Expect(_addPage.TitleField).ToBeVisibleAsync();
    }
}
