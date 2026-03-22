using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.PageObjects.Sales;

/// <summary>
/// /sales — Satış Noktası (POS) sayfasının POM'u.
/// Barkod okutma, sepete ekleme, ödeme tamamlama akışı.
/// </summary>
public class SalesPage(IPage page, string baseUrl)
{
    public ILocator PageTitle => page.GetByText("Satış Noktası (POS)");
    public ILocator BarcodeField => page.GetByLabel("Barkod Okut veya Ürün Ara");
    public ILocator CartTitle => page.GetByText("Sepet");
    public ILocator CartRows => page.Locator(".mud-table-body tr");
    public ILocator CustomerSelectButton => page.GetByRole(AriaRole.Button, new() { Name = "Müşteri Seç" });

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/sales");
        await PageTitle.WaitForAsync(new() { Timeout = 15000 });
    }

    /// <summary>
    /// Barkod alanına değer yaz ve Enter'a bas (ürün arama tetiklenir).
    /// </summary>
    public async Task SearchBarcodeAsync(string barcode)
    {
        await BarcodeField.FillAsync(barcode);
        await BarcodeField.PressAsync("Enter");
        await page.WaitForBlazorRenderAsync();
    }

    /// <summary>
    /// Sepetteki ürün sayısını döndür.
    /// </summary>
    public async Task<int> GetCartItemCountAsync()
    {
        return await CartRows.CountAsync();
    }
}
