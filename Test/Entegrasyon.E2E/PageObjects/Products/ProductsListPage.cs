namespace Entegrasyon.E2E.PageObjects.Products;

/// <summary>
/// /products — Ürün listesi sayfasının POM'u.
/// </summary>
public class ProductsListPage(IPage page, string baseUrl)
{
    public ILocator AddProductButton => page.GetByRole(AriaRole.Button, new() { Name = "Yeni Ürün Ekle" });
    public ILocator DataGrid => page.Locator(".mud-table");
    public ILocator DataGridRows => page.Locator(".mud-table-body tr.mud-table-row");

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/products");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task<int> GetProductCountAsync()
    {
        return await DataGridRows.CountAsync();
    }
}
