using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.PageObjects.Products;

/// <summary>
/// /products/add — 5-adımlı ürün ekleme wizard'ının POM'u.
/// Adımlar: Genel (0) → Varyant Seçimi (1) → Varyant Detayları (2) → Özet (3) → Pazaryeri (4)
/// </summary>
public class AddProductPage(IPage page, string baseUrl)
{
    // Step 0 — Genel Bilgiler
    public ILocator TitleField => page.GetByLabel("Ürün Başlığı");
    public ILocator StockCodeField => page.GetByLabel("Stok Kodu");
    public ILocator BrandAutocomplete => page.GetByLabel("Marka");
    public ILocator CategoryAutocomplete => page.GetByLabel("Kategori");
    public ILocator SeasonField => page.GetByLabel("Sezon");
    public ILocator YearField => page.GetByLabel("Yıl");
    public ILocator DescriptionField => page.GetByLabel("Açıklama");

    // Navigation
    public ILocator NextButton => page.GetByRole(AriaRole.Button, new() { Name = "İleri" });
    public ILocator PrevButton => page.GetByRole(AriaRole.Button, new() { Name = "Geri" });
    public ILocator SaveButton => page.GetByRole(AriaRole.Button, new() { Name = "Kaydet" });
    public ILocator SavingIndicator => page.GetByText("Kaydediliyor...");

    // Page title
    public ILocator PageTitle => page.GetByText("Yeni Ürün Ekle");

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/products/add");
        await PageTitle.WaitForAsync(new() { Timeout = 15000 });
    }

    /// <summary>
    /// Step 0: Genel bilgileri doldur. Marka ve Kategori autocomplete ile seçilir.
    /// </summary>
    public async Task FillGeneralInfoAsync(string title, string stockCode, string brand, string category)
    {
        await TitleField.FillAsync(title);
        await StockCodeField.FillAsync(stockCode);

        // Marka autocomplete
        await MudBlazorHelpers.FillMudAutocompleteAsync(
            page, page.Locator(".mud-autocomplete").First, brand, brand);

        // Kategori autocomplete
        await MudBlazorHelpers.FillMudAutocompleteAsync(
            page, page.Locator(".mud-autocomplete").Nth(1), category, category);

        // Kategori seçildikten sonra attribute'ların yüklenmesini bekle
        await page.WaitForBlazorRenderAsync();
    }

    /// <summary>
    /// İleri butonuna tıkla ve Blazor render'ını bekle.
    /// </summary>
    public async Task ClickNextAsync()
    {
        await NextButton.ClickAndWaitForBlazorAsync(page);
    }

    /// <summary>
    /// Kaydet butonuna tıkla ve snackbar sonucunu döndür.
    /// </summary>
    public async Task<string> ClickSaveAsync()
    {
        await SaveButton.ClickAsync();
        return await page.WaitForSnackbarAsync();
    }
}
