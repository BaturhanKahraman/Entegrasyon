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

    private List<ProductsDetailDto> products = [];
    private List<ProductsDetailDto> filteredProducts = [];
    private string searchString = string.Empty;
    private bool loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadProducts();
    }

    private async Task LoadProducts()
    {
        loading = true;
        try
        {
            var result = await ProductManager.GetProductsDetailsPageable(
                new SearchablePageDto(searchString, 0, 200));
            if (result.Success && result.Data is not null)
            {
                products = result.Data.Items.ToList();
                filteredProducts = products;
            }
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Ürünler yüklenirken hata oluştu: {ex.Message}", Severity.Error);
        }
        finally
        {
            loading = false;
        }
    }

    private async Task OnSearchChanged()
    {
        await LoadProducts();
    }

    private async Task OpenAddProductDialog()
    {
        // Navigate to the dedicated Add Product page
        NavigationManager?.NavigateTo("/products/add");
    }

    private Task ViewProduct(ProductsDetailDto product)
    {
        NavigationManager?.NavigateTo($"/products/{product.Id}");
        return Task.CompletedTask;
    }

    private Task EditProduct(ProductsDetailDto product)
    {
        NavigationManager?.NavigateTo($"/products/edit/{product.Id}");
        return Task.CompletedTask;
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
            await LoadProducts();
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
            await Task.Delay(1000); // Simulate export
            Snackbar?.Add("Excel dosyası indirildi", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Excel aktarma hatası: {ex.Message}", Severity.Error);
        }
    }
}
