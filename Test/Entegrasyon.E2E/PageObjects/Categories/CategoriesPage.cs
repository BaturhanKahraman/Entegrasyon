using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.PageObjects.Categories;

/// <summary>
/// /categories — Kategori yönetim sayfasının POM'u.
/// Sol panel: ağaç listesi, Sağ panel: detay.
/// Tabler UI: .col-lg-4 kolonlar, .modal dialog, Bootstrap ağaç yapısı.
/// </summary>
public class CategoriesPage(IPage page, string baseUrl)
{
    public ILocator PageTitle => page.GetByText("Kategori Yönetimi");
    public ILocator TreePanel => page.Locator(".col-lg-4").First;
    public ILocator DetailPanel => page.Locator(".col-lg-4").Nth(1);

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/categories");
        await PageTitle.WaitForAsync(new() { Timeout = 15000 });
    }

    /// <summary>
    /// "Yeni Kategori Ekle" modal'ını aç.
    /// </summary>
    public async Task ClickAddCategoryAsync()
    {
        var addButton = page.GetByRole(AriaRole.Button, new() { Name = "Ekle" });
        await addButton.ClickAsync();
    }

    /// <summary>
    /// Kategori modal'ında isim girip kaydet.
    /// </summary>
    public async Task FillAndSaveCategoryDialogAsync(string name)
    {
        var modal = page.Locator(".modal.show");
        await modal.WaitForAsync(new() { Timeout = 5000 });

        await page.GetByLabel("Kategori Adı").FillAsync(name);

        await page.ConfirmModalAsync();
    }

    /// <summary>
    /// Ağaçta belirtilen kategori isminin görünüp görünmediğini kontrol et.
    /// </summary>
    public async Task<bool> IsCategoryVisibleInTreeAsync(string categoryName)
    {
        var categoryNode = page.GetByText(categoryName, new() { Exact = false });
        return await categoryNode.IsVisibleAsync();
    }

    /// <summary>
    /// Ağaçtan bir kategori seç (tıkla).
    /// </summary>
    public async Task SelectCategoryAsync(string categoryName)
    {
        await page.GetByText(categoryName, new() { Exact = false }).ClickAsync();
        await page.WaitForHtmxSettleAsync();
    }
}
