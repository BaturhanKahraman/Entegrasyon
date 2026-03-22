using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.PageObjects.Products;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// Ürün ekleme sırasında marka ve kategori seçim davranışlarını test eder.
/// - Marka: Olmayan marka yazıldığında popup ile ekleme
/// - Kategori: Sadece arama, listede olmayan değer seçilemez
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

    /// <summary>
    /// MudAutocomplete'e olmayan marka yazıp Tab ile çıkınca ValueChanged tetiklenir.
    /// Tab sonrası popover kapanır ve blur tetiklenir — ValueChanged(0) çağrılır.
    /// </summary>
    private async Task TypeUnknownBrandAndBlur(string brandName)
    {
        var brandInput = _addPage.BrandAutocomplete;
        await brandInput.ClearAsync();
        await brandInput.FillAsync(brandName);
        await Page.WaitForTimeoutAsync(500); // debounce
        // Tab ile sonraki alana geç — bu blur + ValueChanged tetikler
        await brandInput.PressAsync("Tab");
        await Page.WaitForBlazorRenderAsync();
    }

    [Test]
    public async Task CategoryAutocomplete_DoesNotAllowFreeText()
    {
        // Arrange — olmayan bir kategori adı yaz
        var categoryInput = _addPage.CategoryAutocomplete;
        await categoryInput.FillAsync("OlmayanKategori12345");
        await Page.WaitForTimeoutAsync(500);

        // Escape ile popover'ı kapat, sonra Tab ile blur tetikle
        await categoryInput.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);
        await categoryInput.PressAsync("Tab");
        await Page.WaitForBlazorRenderAsync();
        await Page.WaitForTimeoutAsync(500);

        // Assert — olmayan kategori ile İleri butonuna basınca validasyon hatası alınmalı
        // (CoerceValue=true categoryId'yi 0'a sıfırlar)
        await _addPage.ClickNextAsync();
        var snackbar = Page.Locator(".mud-snackbar");
        await Expect(snackbar).ToBeVisibleAsync(new() { Timeout = 5000 });
        var snackText = await snackbar.TextContentAsync();
        Assert.That(snackText, Does.Contain("gerekli").Or.Contain("doldurun").Or.Contain("kategori").Or.Contain("seçin"),
            "Olmayan kategori ile ileri gidilememeli");
    }

    [Test]
    public async Task NewBrandPopup_WhenTypedBrandNotFound_ShowsConfirmDialog()
    {
        // Arrange + Act
        await TypeUnknownBrandAndBlur("TestYeniMarka_" + DateTime.Now.Ticks);

        // Assert — MessageBox dialog'u görünmeli
        var dialog = Page.Locator(".mud-dialog");
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 10000 });

        var dialogText = await dialog.TextContentAsync();
        Assert.That(dialogText, Does.Contain("bulunmamaktadır"));
        Assert.That(dialogText, Does.Contain("eklemek ister misiniz"));

        // Cleanup — Hayır'a tıkla
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Hayır" }).ClickAsync();
    }

    [Test]
    public async Task NewBrandPopup_ConfirmYes_CreatesBrandAndShowsWarning()
    {
        // Arrange + Act
        var uniqueBrandName = $"E2E_Marka_{DateTime.Now.Ticks}";
        await TypeUnknownBrandAndBlur(uniqueBrandName);

        // Dialog görünmeli
        var dialog = Page.Locator(".mud-dialog");
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Evet'e tıkla
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Evet, Ekle" }).ClickAsync();
        await Page.WaitForBlazorRenderAsync();

        // Assert — Snackbar'da başarı + eşleştirme uyarısı
        var snackbar = Page.Locator(".mud-snackbar");
        await Expect(snackbar).ToBeVisibleAsync(new() { Timeout = 10000 });
        var snackText = await snackbar.TextContentAsync();
        Assert.That(snackText, Does.Contain("eklendi"));
        Assert.That(snackText, Does.Contain("eşleştirmesi"));
    }

    [Test]
    public async Task NewBrandPopup_ConfirmNo_ClearsSelection()
    {
        // Arrange + Act
        await TypeUnknownBrandAndBlur("SilinecekMarka_" + DateTime.Now.Ticks);

        var dialog = Page.Locator(".mud-dialog");
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Hayır'a tıkla
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Hayır" }).ClickAsync();
        await Page.WaitForBlazorRenderAsync();

        // Assert — marka alanı temizlenmiş olmalı
        var brandInput = _addPage.BrandAutocomplete;
        var inputValue = await brandInput.InputValueAsync();
        Assert.That(inputValue, Is.Empty.Or.EqualTo(""), "Marka alanı temizlenmiş olmalı");
    }
}
