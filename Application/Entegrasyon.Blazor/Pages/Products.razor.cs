namespace Entegrasyon.Blazor.Pages;

using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Blazor.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class Products
{
    [Inject]
    private ISnackbar? Snackbar { get; set; }

    [Inject]
    private IDialogService? DialogService { get; set; }

    private List<Product> _products = new();
    private List<Product> _filteredProducts = new();
    private string _searchString = string.Empty;
    private bool _loading = true;
    private bool _showFilters = false;
    private int? _filterBrandId;
    private int? _filterCategoryId;

    protected override async Task OnInitializedAsync()
    {
        await LoadProducts();
    }

    private async Task LoadProducts()
    {
        _loading = true;
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
            _products = new List<Product>();
            _filteredProducts = _products;

            Snackbar?.Add("Ürünler yüklendi", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Ürünler yüklenirken hata oluştu: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnSearchChanged()
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        _filteredProducts = _products
            .Where(p =>
            {
                if (!string.IsNullOrWhiteSpace(_searchString))
                {
                    var searchLower = _searchString.ToLower();
                    if (!(p.Title?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) == true ||
                          p.StockCode?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) == true))
                        return false;
                }

                if (_filterBrandId.HasValue && p.BrandId != _filterBrandId.Value)
                    return false;

                if (_filterCategoryId.HasValue && p.CategoryId != _filterCategoryId.Value)
                    return false;

                return true;
            })
            .ToList();
    }

    private void ToggleFilters()
    {
        _showFilters = !_showFilters;
    }

    private async Task OpenAddProductDialog()
    {
        var parameters = new DialogParameters<ProductDialog>
        {
            { x => x.IsEditMode, false }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService!.ShowAsync<ProductDialog>(
            "Yeni Ürün Ekle",
            parameters,
            options
        );
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadProducts();
        }
    }

    private Task ViewProduct(Product product)
    {
        Snackbar?.Add($"Ürün detayları: {product.Title}", Severity.Info);
        // TODO: Navigate to product detail page or show detail dialog
        return Task.CompletedTask;
    }

    private async Task EditProduct(Product product)
    {
        var parameters = new DialogParameters<ProductDialog>
        {
            { x => x.Product, product },
            { x => x.IsEditMode, true }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService!.ShowAsync<ProductDialog>(
            $"Ürün Düzenle: {product.Title}",
            parameters,
            options
        );
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadProducts();
        }
    }

    private async Task DuplicateProduct(Product product)
    {
        var confirm = await DialogService!.ShowMessageBox(
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
