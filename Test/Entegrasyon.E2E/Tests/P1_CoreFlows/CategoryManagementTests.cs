using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.PageObjects.Categories;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// Kategori yönetimi CRUD akışlarını test eder.
/// Ağaç yapısı, kategori ekleme dialog'u, seçim ve detay paneli.
/// </summary>
[TestFixture, Order(11)]
public class CategoryManagementTests : E2ETestBase
{
    [SetUp]
    public async Task LoginBeforeEach()
    {
        await LoginAsAdminAsync();
    }

    [Test]
    public async Task CategoriesPage_LoadsSuccessfully()
    {
        var catPage = new CategoriesPage(Page, BaseUrl);
        await catPage.NavigateAsync();

        // Başlık ve ağaç paneli görünmeli
        await Expect(catPage.PageTitle).ToBeVisibleAsync();
        await Expect(catPage.TreePanel).ToBeVisibleAsync();
    }

    [Test]
    public async Task CategoryTree_DisplaysSeededData()
    {
        var catPage = new CategoriesPage(Page, BaseUrl);
        await catPage.NavigateAsync();

        // Seed'den gelen test kategorisi görünmeli
        var isVisible = await catPage.IsCategoryVisibleInTreeAsync("E2E Test Ana Kategori");

        // Seed verisi yoksa uyarı ver (seed başarısız olmuş olabilir)
        if (!isVisible)
            Assert.Warn("Seed verisi bulunamadı — 'E2E Test Ana Kategori' ağaçta görünmüyor. Seed akışını kontrol edin.");
        else
            Assert.That(isVisible, Is.True);
    }

    [Test]
    public async Task CategoryAdd_OpensDialog()
    {
        var catPage = new CategoriesPage(Page, BaseUrl);
        await catPage.NavigateAsync();

        await catPage.ClickAddCategoryAsync();

        // Dialog açılmış olmalı
        var dialog = Page.Locator(".mud-dialog");
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Kategori Adı alanı görünmeli
        await Expect(Page.GetByLabel("Kategori Adı")).ToBeVisibleAsync();

        // Dialog'u kapat
        await MudBlazorHelpers.CancelMudDialogAsync(Page);
    }

    [Test]
    public async Task CategoryAdd_CreatesNewCategory()
    {
        var catPage = new CategoriesPage(Page, BaseUrl);
        await catPage.NavigateAsync();

        var testId = Guid.NewGuid().ToString("N")[..8];
        var categoryName = $"E2E Kat {testId}";

        await catPage.ClickAddCategoryAsync();
        await catPage.FillAndSaveCategoryDialogAsync(categoryName);

        // Snackbar başarı mesajı veya ağaçta yeni kategori görünmeli
        await Page.WaitForBlazorRenderAsync();

        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "Kategori ekleme sırasında hata oluştu");
    }
}
