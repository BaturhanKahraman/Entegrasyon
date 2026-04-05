using Microsoft.Playwright;

namespace Entegrasyon.E2E.Infrastructure;

public static class HtmxWaitHelpers
{
    /// <summary>
    /// HTMX partial swap tamamlanana kadar bekle.
    /// NetworkIdle + htmx-request class'ının kalkmasi.
    /// </summary>
    public static async Task WaitForHtmxSettleAsync(this IPage page, float timeoutMs = 5000)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        try
        {
            await page.WaitForFunctionAsync(
                "() => !document.querySelector('.htmx-request')",
                null,
                new PageWaitForFunctionOptions { Timeout = timeoutMs });
        }
        catch (TimeoutException) { }
    }

    /// <summary>
    /// Tiklayip HTMX swap'in tamamlanmasini bekle.
    /// </summary>
    public static async Task ClickAndWaitForHtmxAsync(this ILocator locator, IPage page, float timeoutMs = 5000)
    {
        await locator.ClickAsync();
        await WaitForHtmxSettleAsync(page, timeoutMs);
    }

    /// <summary>
    /// Sayfada hata olup olmadigini kontrol et.
    /// MVC'de .alert-danger veya hata sayfasi kontrolu.
    /// </summary>
    public static async Task<bool> HasNoErrorAsync(this IPage page)
    {
        // Check for error page (500, etc.)
        var errorPage = page.Locator("h1:text('Bir hata olustu'), h1:text('500')");
        var hasErrorPage = await errorPage.CountAsync() > 0;

        return !hasErrorPage;
    }

    /// <summary>
    /// HTMX showToast trigger'indan gelen toast mesajini bekle.
    /// site.js #toast-container'a .alert ekliyor.
    /// </summary>
    public static async Task<string> WaitForToastAsync(this IPage page, float timeoutMs = 10000)
    {
        var toast = page.Locator("#toast-container .alert");
        await toast.First.WaitForAsync(new() { Timeout = timeoutMs });
        return await toast.First.InnerTextAsync();
    }
}
