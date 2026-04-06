using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.PageObjects.Brands;

/// <summary>
/// /brands — Markalar listesi sayfasinin POM'u.
/// </summary>
public class BrandsPage(IPage page, string baseUrl)
{
    public ILocator PageTitle => page.GetByRole(AriaRole.Heading, new() { Name = "Markalar" });
    public ILocator ImportButton => page.GetByRole(AriaRole.Link, new() { Name = "Ice Aktar" });
    public ILocator AddBrandButton => page.GetByRole(AriaRole.Button, new() { Name = "Yeni Marka" });
    public ILocator SearchInput => page.GetByPlaceholder("Marka ara...");
    public ILocator BrandTable => page.Locator("#brand-table");

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/brands");
        await PageTitle.WaitForAsync(new() { Timeout = 10000 });
    }

    public async Task<int> GetBrandCountAsync()
    {
        var rows = page.Locator("#brand-table tbody tr");
        return await rows.CountAsync();
    }

    public async Task<bool> HasBrandAsync(string brandName)
    {
        var brand = page.GetByRole(AriaRole.Link, new() { Name = brandName, Exact = true });
        return await brand.CountAsync() > 0;
    }
}
