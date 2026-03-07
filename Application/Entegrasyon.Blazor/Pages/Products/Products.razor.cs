namespace Entegrasyon.Blazor.Pages.Products;

using Entegrasyon.Entity.Products;
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
    private List<Product> products = [];
    private List<Product> filteredProducts = [];
    private string searchString = string.Empty;
    private bool loading = true;
    private bool showFilters = false;
    private int? filterBrandId;
    private int? filterCategoryId;

    protected override async Task OnInitializedAsync()
    {
        await LoadProducts();
    }

    private async Task LoadProducts()
    {
        loading = true;
        try
        {
            // TODO: Implement actual product loading from ProductManager
            // var result = await ProductManager.GetAllProductsAsync();
            // if (result.Success)
            // {
            //     _products = result.Data.ToList();
            //     ApplyFilters();
            // }

            // Mock data for demonstration
            products = [];
            filteredProducts = products;

            Snackbar?.Add("Ürünler yüklendi", Severity.Success);
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

    private void OnSearchChanged()
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        filteredProducts = products
            .Where(p =>
            {
                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    var searchLower = searchString.ToLower();
                    if (!(p.Title?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) == true ||
                          p.StockCode?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) == true))
                        return false;
                }

                if (filterBrandId.HasValue && p.BrandId != filterBrandId.Value)
                    return false;

                if (filterCategoryId.HasValue && p.CategoryId != filterCategoryId.Value)
                    return false;

                return true;
            })
            .ToList();
    }

    private void ToggleFilters()
    {
        showFilters = !showFilters;
    }

    private async Task OpenAddProductDialog()
    {
        // Navigate to the dedicated Add Product page
        NavigationManager?.NavigateTo("/products/add");
    }

    private Task ViewProduct(Product product)
    {
        Snackbar?.Add($"Ürün detayları: {product.Title}", Severity.Info);
        // TODO: Navigate to product detail page or show detail dialog
        return Task.CompletedTask;
    }

    private async Task EditProduct(Product product)
    {
        // Navigate to edit page (to be implemented). For now route to a placeholder edit path.
        NavigationManager?.NavigateTo($"/products/edit/{product.Id}");
    }

    private async Task DuplicateProduct(Product product)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Ürün Kopyala",
            $"'{product.Title}' ürününü kopyalamak istediğinize emin misiniz?",
            yesText: "Evet",
            cancelText: "İptal"
        );

        if (confirm == true)
        {
            // TODO: Implement product duplication
            Snackbar?.Add($"Ürün kopyalandı: {product.Title}", Severity.Success);
            await LoadProducts();
        }
    }

    private async Task DeleteProduct(Product product)
    {
        var confirm = await DialogService!.ShowMessageBox(
            "Uyarı",
            $"'{product.Title}' ürününü silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            yesText: "Sil",
            cancelText: "İptal"
        );

        if (confirm == true)
        {
            try
            {
                // TODO: Implement actual deletion
                // var result = await ProductManager.DeleteProduct(product.Id);
                // if (result.Success)
                // {
                //     Snackbar.Add(result.Message, Severity.Success);
                //     await LoadProducts();
                //     await ProductEventChannel.PublishAsync(new ProductUpdatedEvent(product.Id, "Deleted"));
                // }

                Snackbar?.Add($"Ürün silindi: {product.Title} (Mock)", Severity.Info);
            }
            catch (Exception ex)
            {
                Snackbar?.Add($"Ürün silinirken hata: {ex.Message}", Severity.Error);
            }
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
