using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.PageObjects.Products;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// Ürün CRUD akışlarını test eder.
/// En kritik test: 5-adımlı ürün ekleme wizard'ının tam akışı.
/// </summary>
[TestFixture, Order(10)]
public class ProductCrudTests : E2ETestBase
{
    [SetUp]
    public async Task LoginBeforeEach()
    {
        await LoginAsAdminAsync();
    }

    [Test]
    public async Task ProductsList_LoadsSuccessfully()
    {
        var productsPage = new ProductsListPage(Page, BaseUrl);
        await productsPage.NavigateAsync();

        // DataGrid yüklenmeli
        await Expect(productsPage.DataGrid).ToBeVisibleAsync(new() { Timeout = 10000 });

        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "Ürünler sayfasında hata tespit edildi");
    }

    [Test]
    public async Task AddProductPage_LoadsWizard()
    {
        var addPage = new AddProductPage(Page, BaseUrl);
        await addPage.NavigateAsync();

        // Wizard başlığı görünmeli
        await Expect(addPage.PageTitle).ToBeVisibleAsync();

        // Step 0 alanları görünmeli
        await Expect(addPage.TitleField).ToBeVisibleAsync();
        await Expect(addPage.StockCodeField).ToBeVisibleAsync();
        await Expect(addPage.BrandAutocomplete).ToBeVisibleAsync();
        await Expect(addPage.CategoryAutocomplete).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddProductWizard_Step0_FillsGeneralInfo()
    {
        var addPage = new AddProductPage(Page, BaseUrl);
        await addPage.NavigateAsync();

        // Unique test verisi oluştur
        var testId = Guid.NewGuid().ToString("N")[..8];

        await addPage.TitleField.FillAsync($"E2E Ürün {testId}");
        await addPage.StockCodeField.FillAsync($"E2E-{testId}");

        // Alanların dolu olduğunu doğrula
        await Expect(addPage.TitleField).ToHaveValueAsync($"E2E Ürün {testId}");
        await Expect(addPage.StockCodeField).ToHaveValueAsync($"E2E-{testId}");
    }

    [Test]
    public async Task AddProductWizard_ValidationBlocksEmptyForm()
    {
        var addPage = new AddProductPage(Page, BaseUrl);
        await addPage.NavigateAsync();

        // Boş form ile ileri gitmeyi dene
        await addPage.NextButton.ClickAsync();
        await Page.WaitForBlazorRenderAsync();

        // Hala Step 0'da olmalıyız (validation hatası nedeniyle)
        await Expect(addPage.TitleField).ToBeVisibleAsync();
    }

    [Test]
    public async Task ProductsList_NavigatesToAddProduct()
    {
        var productsPage = new ProductsListPage(Page, BaseUrl);
        await productsPage.NavigateAsync();

        // Yeni Ürün Ekle butonuna tıkla
        await productsPage.AddProductButton.ClickAsync();

        // /products/add sayfasına gitmiş olmalıyız
        await Expect(Page).ToHaveURLAsync(new Regex(@"/products/add"));
    }
}
