using System.Globalization;

namespace Entegrasyon.E2E.Infrastructure;

/// <summary>
/// MudBlazor bileşenleriyle etkileşim yardımcıları.
/// MudBlazor overlay/popover tabanlı DOM üretir — standart Playwright selector'ları yetmez.
/// Bu helper'lar MudBlazor güncellenmesi durumunda tek noktadan düzeltme sağlar.
/// </summary>
public static class MudBlazorHelpers
{
    /// <summary>
    /// MudTextField'a değer gir ve validation'ın tetiklenmesini sağla.
    /// Immediate="true" olan alanlarda FillAsync yeterli.
    /// Immediate="false" (varsayılan) olanlarda FillAsync + BlurAsync gerekir.
    /// </summary>
    public static async Task FillMudInputAsync(this ILocator locator, string value, bool immediate = true)
    {
        await locator.FillAsync(value);
        if (!immediate)
            await locator.BlurAsync();
    }

    /// <summary>
    /// MudSelect bileşeninden bir değer seç.
    /// MudSelect tıklandığında .mud-popover-open içinde seçenekleri gösterir.
    /// </summary>
    public static async Task SelectMudSelectValueAsync(
        IPage page,
        ILocator selectLocator,
        string optionText)
    {
        await selectLocator.ClickAsync();

        var popover = page.Locator(".mud-popover-open");
        await popover.WaitForAsync(new() { Timeout = 5000 });

        await popover.GetByText(optionText, new() { Exact = false }).ClickAsync();

        // Popover'ın kapanmasını bekle
        await popover.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 3000 });
    }

    /// <summary>
    /// MudAutocomplete bileşenine metin yaz ve sonuçlardan seç.
    /// Debounced arama tetikler, sonuçlar popover'da gösterilir.
    /// </summary>
    public static async Task FillMudAutocompleteAsync(
        IPage page,
        ILocator autocompleteLocator,
        string searchText,
        string selectText)
    {
        var input = autocompleteLocator.Locator("input");
        await input.FillAsync(searchText);

        // Debounce ve API çağrısı için bekle
        var popover = page.Locator(".mud-popover-open");
        await popover.WaitForAsync(new() { Timeout = 10000 });

        await popover.GetByText(selectText, new() { Exact = false }).ClickAsync();
    }

    /// <summary>
    /// MudDialog'da belirtilen butona tıkla ve dialog'un kapanmasını bekle.
    /// </summary>
    public static async Task ConfirmMudDialogAsync(IPage page, string buttonText = "Kaydet")
    {
        var dialog = page.Locator(".mud-dialog");
        await dialog.WaitForAsync(new() { Timeout = 5000 });

        await dialog.GetByRole(AriaRole.Button, new() { Name = buttonText }).ClickAsync();

        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 10000 });
    }

    /// <summary>
    /// MudDialog'u iptal et / kapat.
    /// </summary>
    public static async Task CancelMudDialogAsync(IPage page, string buttonText = "İptal")
    {
        var dialog = page.Locator(".mud-dialog");
        await dialog.WaitForAsync(new() { Timeout = 5000 });

        await dialog.GetByRole(AriaRole.Button, new() { Name = buttonText }).ClickAsync();

        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
    }

    /// <summary>
    /// MudDataGrid'deki satır sayısını döndür.
    /// </summary>
    public static async Task<int> GetDataGridRowCountAsync(IPage page, ILocator? gridLocator = null)
    {
        var grid = gridLocator ?? page.Locator(".mud-table");
        var rows = grid.Locator("tbody tr.mud-table-row");
        return await rows.CountAsync();
    }

    /// <summary>
    /// MudTextField'a değer gir (label ile bul).
    /// </summary>
    public static async Task FillMudTextFieldAsync(IPage page, string label, string value)
    {
        await page.GetByLabel(label).FillAsync(value);
    }

    /// <summary>
    /// MudNumericField'a sayısal değer gir (label ile bul).
    /// </summary>
    public static async Task FillMudNumericFieldAsync(IPage page, string label, decimal value)
    {
        var field = page.GetByLabel(label);
        await field.ClearAsync();
        await field.FillAsync(value.ToString(CultureInfo.InvariantCulture));
    }
}
