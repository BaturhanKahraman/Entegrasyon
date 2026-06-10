using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.PageObjects.Brands;

/// <summary>
/// /brands/master-import — Master katalogdan marka aktarma sayfasinin POM'u.
/// Sol: HTMX arama, Sag: JS secim sepeti + import form.
/// </summary>
public class BrandMasterImportPage(IPage page, string baseUrl)
{
    public ILocator PageTitle => page.GetByRole(AriaRole.Heading, new() { Name = "Master Katalogdan Marka Aktarma" }).First;
    public ILocator SearchInput => page.GetByPlaceholder("Marka adi yazin (en az 2 karakter)...");
    public ILocator SearchResults => page.Locator("#search-results");
    public ILocator SelectedBrands => page.Locator("#selected-brands");
    public ILocator SelectionCount => page.Locator("#selectionCount");
    public ILocator ImportButton => page.Locator("#importBtn");
    public ILocator ClearButton => page.Locator("#clearBtn");
    public ILocator ImportResultArea => page.Locator("#import-result-area");
    public ILocator BackButton => page.GetByRole(AriaRole.Link, new() { Name = "Markalara Don" }).First;

    // Import result elements
    public ILocator SuccessCard => ImportResultArea.Locator(".border-success");
    public ILocator ErrorAlert => ImportResultArea.Locator(".alert-danger");
    public ILocator BrandsImportedCount => SuccessCard.Locator(".card-body h3.text-primary");
    public ILocator BrandsSkippedCount => SuccessCard.Locator(".card-body h3.text-secondary");
    public ILocator GoToBrandsLink => ImportResultArea.GetByRole(AriaRole.Link, new() { Name = "Markalara Git" });
    public ILocator RetryImportLink => ImportResultArea.GetByRole(AriaRole.Link, new() { Name = "Tekrar Import" });

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/brands/master-import");
        await PageTitle.WaitForAsync(new() { Timeout = 10000 });
    }

    /// <summary>
    /// Arama kutusuna yazar ve HTMX sonuclarini bekler.
    /// HTMX trigger "keyup changed delay:400ms" oldugu icin FillAsync yerine
    /// PressSequentiallyAsync kullanarak keyup event'lerini tetikleriz.
    /// </summary>
    public async Task SearchAsync(string query)
    {
        await SearchInput.ClickAsync();
        await SearchInput.FillAsync("");
        await SearchInput.PressSequentiallyAsync(query, new() { Delay = 50 });
        // HTMX delay:400ms + network
        await page.WaitForTimeoutAsync(800);
        await page.WaitForHtmxSettleAsync();
    }

    /// <summary>
    /// Arama sonuclarindan bir markayi secer (+ butonuna tiklar).
    /// HasText partial match yaptigi icin birden fazla sonuc eslesebilir.
    /// Ilk eslesen satirdan secim yapar.
    /// </summary>
    public async Task SelectBrandFromResultsAsync(string brandName)
    {
        var resultRow = SearchResults.Locator(".list-group-item").Filter(new() { HasText = brandName }).First;
        var addButton = resultRow.GetByRole(AriaRole.Button);
        await addButton.ClickAsync();
    }

    /// <summary>
    /// Secili marka Sayısıni doner.
    /// </summary>
    public async Task<int> GetSelectedCountAsync()
    {
        var text = await SelectionCount.InnerTextAsync();
        return int.TryParse(text, out var count) ? count : 0;
    }

    /// <summary>
    /// Import butonuna tiklar ve sonucu bekler.
    /// </summary>
    public async Task ClickImportAsync()
    {
        await ImportButton.ClickAsync();
        // HTMX post -> result partial
        await page.WaitForHtmxSettleAsync(timeoutMs: 15000);
    }

    /// <summary>
    /// Secim sepetinde belirtilen markanin olup olmadigini kontrol eder.
    /// </summary>
    public async Task<bool> IsSelectedAsync(string brandName)
    {
        var selected = SelectedBrands.Locator(".list-group-item").Filter(new() { HasText = brandName });
        return await selected.CountAsync() > 0;
    }

    /// <summary>
    /// Secim sepetinden bir markayi kaldirir (x butonuna tiklar).
    /// </summary>
    public async Task RemoveBrandAsync(string brandName)
    {
        var selected = SelectedBrands.Locator(".list-group-item").Filter(new() { HasText = brandName });
        var removeButton = selected.GetByRole(AriaRole.Button);
        await removeButton.ClickAsync();
    }
}
