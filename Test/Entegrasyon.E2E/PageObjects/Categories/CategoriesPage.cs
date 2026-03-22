using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.PageObjects.Categories;

/// <summary>
/// /categories — Kategori yönetim sayfasının POM'u.
/// Sol panel: CategoryTreePanel (ağaç), Sağ panel: CategoryDetailsPanel (detay).
/// </summary>
public class CategoriesPage(IPage page, string baseUrl)
{
    public ILocator PageTitle => page.GetByText("Kategori Yönetimi");
    public ILocator TreePanel => page.Locator(".mud-grid-item").First;
    public ILocator DetailPanel => page.Locator(".mud-grid-item").Nth(1);

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/categories");
        await PageTitle.WaitForAsync(new() { Timeout = 15000 });
    }

    /// <summary>
    /// "Yeni Kategori Ekle" dialog'unu aç.
    /// CategoryTreePanel içindeki Ekle butonuna tıklar.
    /// </summary>
    public async Task ClickAddCategoryAsync()
    {
        // TreePanel içindeki add butonunu bul
        var addButton = page.GetByRole(AriaRole.Button, new() { Name = "Ekle" });
        await addButton.ClickAsync();
    }

    /// <summary>
    /// Kategori dialog'unda isim girip kaydet.
    /// </summary>
    public async Task FillAndSaveCategoryDialogAsync(string name)
    {
        var dialog = page.Locator(".mud-dialog");
        await dialog.WaitForAsync(new() { Timeout = 5000 });

        await page.GetByLabel("Kategori Adı").FillAsync(name);

        await MudBlazorHelpers.ConfirmMudDialogAsync(page, "Kaydet");
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
        await page.WaitForBlazorRenderAsync();
    }
}
