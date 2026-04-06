namespace Entegrasyon.E2E.PageObjects.Brands;

/// <summary>
/// /marketplace/sync — Pazaryeri senkronizasyon genel bakis sayfasi.
/// Marketplace kartlarindaki butonlari test etmek icin.
/// </summary>
public class SyncOverviewPage(IPage page, string baseUrl)
{
    public ILocator PageTitle => page.GetByRole(AriaRole.Heading, new() { Name = "Pazaryeri Senkronizasyon" });
    public ILocator BrandMappingLinks => page.GetByRole(AriaRole.Link, new() { Name = "Marka Eslestir" });

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/marketplace/sync");
        await PageTitle.WaitForAsync(new() { Timeout = 10000 });
    }

    public async Task<int> GetBrandMappingLinkCountAsync()
    {
        return await BrandMappingLinks.CountAsync();
    }

    public ILocator GetBrandMappingLink(int marketPlaceId)
    {
        return page.GetByRole(AriaRole.Link, new() { Name = "Marka Eslestir" })
            .And(page.Locator($"[href*='mp={marketPlaceId}']"));
    }
}
