namespace Entegrasyon.E2E.PageObjects;

/// <summary>
/// Sol navigasyon menüsünün Page Object Model'i.
/// MudNavMenu yapısı: expandable gruplar + MudNavLink'ler.
/// </summary>
public class NavMenuComponent(IPage page)
{
    public ILocator NavDrawer => page.Locator(".mud-drawer");

    /// <summary>
    /// Menüde belirtilen metne sahip link'e tıkla.
    /// </summary>
    public async Task ClickNavLinkAsync(string linkText)
    {
        await NavDrawer.GetByText(linkText, new() { Exact = false }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Expandable grup başlığına tıkla (ör: "Yönetim", "Pazaryeri").
    /// </summary>
    public async Task ExpandGroupAsync(string groupText)
    {
        await NavDrawer.GetByText(groupText, new() { Exact = false }).ClickAsync();
    }

    /// <summary>
    /// Belirtilen nav link'in görünür olup olmadığını kontrol et.
    /// Yetki kontrolü testlerinde kullanılır.
    /// </summary>
    public async Task<bool> IsNavLinkVisibleAsync(string linkText)
    {
        return await NavDrawer.GetByText(linkText, new() { Exact = false }).IsVisibleAsync();
    }
}
