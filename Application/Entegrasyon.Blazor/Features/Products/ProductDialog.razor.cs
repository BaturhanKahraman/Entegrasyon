namespace Entegrasyon.Blazor.Features.Products;

using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class ProductDialog
{
    [CascadingParameter]
    public IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public Product? Product { get; set; }

    [Parameter]
    public bool IsEditMode { get; set; }

    [Inject]
    private ISnackbar? Snackbar { get; set; }

    private MudForm _form = null!;
    private bool _isValid;
    private bool _saving;
    private ProductFormModel _model = new();
    private List<BrandDto> _brands = new();
    private List<CategoryDto> _categories = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadLookupData();

        if (IsEditMode && Product != null)
        {
            _model = new ProductFormModel
            {
                Id = Product.Id,
                StockCode = Product.StockCode ?? "",
                Title = Product.Title ?? "",
                Description = Product.Description,
                BrandId = Product.BrandId ?? 0,
                CategoryId = Product.CategoryId,
                Season = Product.Season,
                Year = Product.Year
            };
        }
        else
        {
            _model.Year = DateTime.Now.Year.ToString();
            AddVariant();
        }
    }

    private Task LoadLookupData()
    {
        // TODO: Implement actual data loading
        // var brandsResult = await BrandManager.GetAllBrandsAsync();
        // var categoriesResult = await CategoryManager.GetAllCategoriesAsync();

        // Mock data
        _brands = new List<BrandDto>();
        _categories = new List<CategoryDto>();
        return Task.CompletedTask;
    }

    private void AddVariant()
    {
        _model.ProductVariants.Add(new ProductVariantDto
        {
            Size = string.Empty,
            Barcode = string.Empty,
            ListPrice = 0,
            BranchOfficeStocks = new List<BranchOfficeStockDto>()
        });
    }

    private void RemoveVariant(int index)
    {
        _model.ProductVariants.RemoveAt(index);
    }

    private async Task Submit()
    {
        await _form.Validate();
        if (!_isValid)
            return;

        _saving = true;
        try
        {
            if (IsEditMode)
            {
                // TODO: Call update method
                // var result = await ProductManager.UpdateProduct(dto);
                Snackbar?.Add("Ürün güncellendi", Severity.Success);
            }
            else
            {
                var dto = new AddProductDto
                {
                    StockCode = _model.StockCode,
                    Title = _model.Title,
                    Description = _model.Description ?? "",
                    BrandId = _model.BrandId,
                    CategoryId = _model.CategoryId
                };

                // TODO: Uncomment when ready
                // var result = await ProductManager.AddProduct(dto);
                // if (result.Success)
                // {
                //     Snackbar.Add(result.Message ?? "", Severity.Success);
                //     MudDialog.Close(DialogResult.Ok(true));
                // }
                // else
                // {
                //     Snackbar.Add(result.Message ?? "", Severity.Error);
                // }

                Snackbar?.Add("Ürün eklendi (Mock)", Severity.Info);
                MudDialog?.Close(DialogResult.Ok(true));
            }
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());

    private class ProductFormModel
    {
        public Guid Id { get; set; }
        public string StockCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int BrandId { get; set; }
        public int CategoryId { get; set; }
        public string? Season { get; set; }
        public string? Year { get; set; }
        public List<ProductVariantDto> ProductVariants { get; set; } = new();
    }

    private record BrandDto(int Id, string Name);
    private record CategoryDto(int Id, string Name);
    private record ProductVariantDto
    {
        public string Size { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public decimal ListPrice { get; set; }
        public List<BranchOfficeStockDto> BranchOfficeStocks { get; set; } = new();
    }
    private record BranchOfficeStockDto;
}
