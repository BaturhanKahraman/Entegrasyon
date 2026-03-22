namespace Entegrasyon.E2E.Infrastructure;

/// <summary>
/// Blazor Server SignalR-aware bekleme yardımcıları.
/// Blazor Server DOM güncellemelerini SignalR üzerinden gönderir —
/// HTTP response beklemek yerine DOM stabilizasyonu beklemek gerekir.
/// </summary>
public static class BlazorWaitHelpers
{
    /// <summary>
    /// Bir elemana tıkla ve Blazor'un SignalR round-trip'ini tamamlamasını bekle.
    /// </summary>
    public static async Task ClickAndWaitForBlazorAsync(
        this ILocator locator,
        IPage page,
        float timeoutMs = 5000)
    {
        await locator.ClickAsync();
        await WaitForBlazorRenderAsync(page, timeoutMs);
    }

    /// <summary>
    /// Blazor render döngüsünün tamamlanmasını bekle.
    /// NetworkIdle + MudBlazor progress indicator kontrolü.
    /// </summary>
    public static async Task WaitForBlazorRenderAsync(this IPage page, float timeoutMs = 5000)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // MudBlazor loading indicator'ların kaybolmasını bekle
        try
        {
            await page.WaitForFunctionAsync(
                "() => !document.querySelector('.mud-progress-circular, .mud-progress-linear, .mud-skeleton')",
                null,
                new PageWaitForFunctionOptions { Timeout = timeoutMs });
        }
        catch (TimeoutException)
        {
            // Timeout olursa devam et — bazı sayfalarda sürekli yükleme göstergesi olabilir
        }
    }

    /// <summary>
    /// MudBlazor Snackbar mesajının görünmesini bekle ve metnini döndür.
    /// </summary>
    public static async Task<string> WaitForSnackbarAsync(this IPage page, float timeoutMs = 10000)
    {
        var snackbar = page.Locator(".mud-snackbar .mud-snackbar-content-message");
        await snackbar.WaitForAsync(new() { Timeout = timeoutMs });
        return await snackbar.InnerTextAsync();
    }

    /// <summary>
    /// Blazor Server SignalR bağlantısının aktif olduğunu doğrula.
    /// Reconnect modal görünmüyorsa bağlantı aktif demektir.
    /// </summary>
    public static async Task WaitForBlazorConnectedAsync(this IPage page, float timeoutMs = 15000)
    {
        await page.WaitForFunctionAsync(
            "() => !document.getElementById('components-reconnect-modal') || document.getElementById('components-reconnect-modal').style.display === 'none'",
            null,
            new PageWaitForFunctionOptions { Timeout = timeoutMs });
    }

    /// <summary>
    /// Sayfada ErrorBoundary veya Blazor hata UI'ının görünmediğini doğrula.
    /// </summary>
    public static async Task<bool> HasNoErrorAsync(this IPage page)
    {
        var blazorError = page.Locator("#blazor-error-ui:visible");
        var errorBoundary = page.Locator("text=Beklenmeyen bir hata oluştu");

        var hasBlazorError = await blazorError.IsVisibleAsync();
        var hasErrorBoundary = await errorBoundary.IsVisibleAsync();

        return !hasBlazorError && !hasErrorBoundary;
    }
}
