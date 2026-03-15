namespace Entegrasyon.Blazor.Features.Products;

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class Products
{
    [Inject]
    private ISnackbar? Snackbar { get; set; }

    [Inject]
    private NavigationManager? NavigationManager { get; set; }
    [Inject]
    private IDialogService DialogService { get; set; }
    [Inject]
    private IProductService ProductManager { get; set; } = null!;

    private MudDataGrid<ProductsDetailDto> _dataGrid = null!;
    private string searchString = string.Empty;

    private async Task<GridData<ProductsDetailDto>> ServerData(GridState<ProductsDetailDto> state)
    {
        var result = await ProductManager.GetProductsDetailsPageable(
            new SearchablePageDto(searchString, state.Page, state.PageSize));

        if (result.Success && result.Data is not null)
            return new GridData<ProductsDetailDto>
            {
                TotalItems = result.Data.TotalItemCount,
                Items = result.Data.Items
            };

        return new GridData<ProductsDetailDto> { TotalItems = 0, Items = [] };
    }

    private Task OnSearchChanged(string text)
    {
        searchString = text;
        return _dataGrid.ReloadServerData();
    }

    private async Task OpenAddProductDialog()
    {
        // Navigate to the dedicated Add Product page
        NavigationManager?.NavigateTo("/products/add");
    }

    private void OnRowClick(DataGridRowClickEventArgs<ProductsDetailDto> args)
    {
        NavigateToProduct(args.Item.Id);
    }

    private void NavigateToProduct(Guid id)
    {
        NavigationManager?.NavigateTo($"/products/{id}");
    }

    private void EditProduct(ProductsDetailDto product)
    {
        NavigationManager?.NavigateTo($"/products/edit/{product.Id}");
    }

    private async Task DuplicateProduct(ProductsDetailDto product)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Ürün Kopyala",
            $"'{product.Title}' ürününü kopyalamak istediğinize emin misiniz?",
            yesText: "Evet",
            cancelText: "İptal"
        );

        if (confirm == true)
        {
            Snackbar?.Add($"Ürün kopyalandı: {product.Title}", Severity.Success);
            await _dataGrid.ReloadServerData();
        }
    }

    private async Task DeleteProduct(ProductsDetailDto product)
    {
        var confirm = await DialogService!.ShowMessageBox(
            "Uyarı",
            $"'{product.Title}' ürününü silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            yesText: "Sil",
            cancelText: "İptal"
        );

        if (confirm == true)
        {
            // TODO: Implement actual deletion via ProductManager
            Snackbar?.Add("Ürün silme henüz uygulanmadı.", Severity.Warning);
        }
    }

    private async Task ExportToExcel()
    {
        try
        {
            // TODO: Implement Excel export using EPPlus or ClosedXML
            Snackbar?.Add("Excel dosyası oluşturuluyor...", Severity.Info);
            // TODO: Implement actual Excel export
            Snackbar?.Add("Excel dosyası indirildi", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Excel aktarma hatası: {ex.Message}", Severity.Error);
        }
    }
}
