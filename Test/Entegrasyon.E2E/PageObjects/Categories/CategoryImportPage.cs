using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.PageObjects.Categories;

/// <summary>
/// /categories/import — Kategori içe aktarma sayfasının POM'u.
/// Trendyol, Hepsiburada ve N11 tab'larını barındırır.
/// </summary>
public class CategoryImportPage(IPage page, string baseUrl)
{
    public ILocator PageTitle => page.GetByText("Kategori İçe Aktar");

    // N11 tab — MudTabPanel içinde "N11" metni
    public ILocator N11Tab => page.Locator(".mud-tab").Filter(new() { HasText = "N11" });

    // N11 tab içeriği paneli — "N11 Kategorileri" başlığı
    public ILocator N11ContentTitle => page.GetByText("N11 Kategorileri");

    // N11 panel içindeki "Kategorileri Yükle" butonu
    // Not: Trendyol ve N11 aynı metni paylaşır; N11 paneli görünür olduğunda
    // görünür olan buton N11'e aittir.
    public ILocator LoadCategoriesButton => page.GetByRole(AriaRole.Button, new() { Name = "Kategorileri Yükle" }).Last;

    // Ağaç görünümü konteyneri (kategoriler yüklendikten sonra görünür)
    public ILocator TreeViewContainer => page.Locator(".mud-treeview");

    // Boş durum uyarısı — "Kategorileri görüntülemek için" metni
    public ILocator EmptyStateAlert => page.Locator(".mud-alert").Filter(new() { HasText = "Kategorileri görüntülemek için" }).Last;

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/categories/import");
        await PageTitle.WaitForAsync(new() { Timeout = 15000 });
    }

    /// <summary>
    /// N11 tab'ına tıkla ve Blazor render'ını bekle.
    /// </summary>
    public async Task ClickN11TabAsync()
    {
        await N11Tab.ClickAsync();
        await page.WaitForBlazorRenderAsync();
    }
}
