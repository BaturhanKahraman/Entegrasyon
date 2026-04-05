namespace Entegrasyon.E2E.PageObjects;

/// <summary>
/// Sol navigasyon menüsünün Page Object Model'i.
/// Tabler UI: aside.navbar-vertical — flat link yapısı, collapsible grup yok.
/// </summary>
public class NavMenuComponent(IPage page)
{
    public ILocator NavDrawer => page.Locator("aside.navbar-vertical");

    /// <summary>
    /// Menüde belirtilen metne sahip link'e tıkla.
    /// </summary>
    public async Task ClickNavLinkAsync(string linkText)
    {
        await NavDrawer.Locator(".nav-link").Filter(new() { HasText = linkText }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Belirtilen nav link'in görünür olup olmadığını kontrol et.
    /// Yetki kontrolü testlerinde kullanılır.
    /// </summary>
    public async Task<bool> IsNavLinkVisibleAsync(string linkText)
    {
        return await NavDrawer.Locator(".nav-link").Filter(new() { HasText = linkText }).IsVisibleAsync();
    }
}
