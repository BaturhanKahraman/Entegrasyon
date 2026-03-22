namespace Entegrasyon.E2E.PageObjects;

/// <summary>
/// / (Dashboard) sayfasının Page Object Model'i.
/// </summary>
public class DashboardPage(IPage page, string baseUrl)
{
    public ILocator PageContent => page.Locator(".mud-main-content");

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<bool> IsLoadedAsync()
    {
        try
        {
            await PageContent.WaitForAsync(new() { Timeout = 10000 });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
