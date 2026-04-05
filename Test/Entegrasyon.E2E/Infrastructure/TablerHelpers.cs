using Microsoft.Playwright;

namespace Entegrasyon.E2E.Infrastructure;

public static class TablerHelpers
{
    public static async Task SelectOptionAsync(this IPage page, string selectSelector, string value)
    {
        await page.SelectOptionAsync(selectSelector, value);
    }

    public static async Task ConfirmModalAsync(this IPage page, float timeoutMs = 5000)
    {
        var modal = page.Locator(".modal.show");
        await modal.WaitForAsync(new() { Timeout = timeoutMs });
        var confirmBtn = modal.Locator(".btn-primary, .btn-danger").First;
        await confirmBtn.ClickAsync();
        await page.WaitForHtmxSettleAsync();
    }

    public static async Task CancelModalAsync(this IPage page)
    {
        var closeBtn = page.Locator(".modal.show .btn-close, .modal.show [data-bs-dismiss='modal']").First;
        await closeBtn.ClickAsync();
    }

    public static async Task<int> GetTableRowCountAsync(this IPage page, string tableSelector = ".table")
    {
        return await page.Locator($"{tableSelector} tbody tr").CountAsync();
    }
}
