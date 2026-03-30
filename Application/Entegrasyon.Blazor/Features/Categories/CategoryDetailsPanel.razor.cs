using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryDetailsPanel
{
    [Inject] private IDbContextFactory<IntegrationDbContext> ContextFactory { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [Parameter] public Category? SelectedCategory { get; set; }
    [Parameter] public int SubcategoryCount { get; set; }
    [Parameter] public int ProductCount { get; set; }
    [Parameter] public EventCallback OnEditClicked { get; set; }
    [Parameter] public EventCallback OnDeleteClicked { get; set; }
    [Parameter] public Func<int, string>? GetParentCategoryNameFunc { get; set; }

    private bool _showProducts;
    private bool _loadingProducts;
    private List<CategoryProductItem> _products = [];
    private int? _lastLoadedCategoryId;

    protected override void OnParametersSet()
    {
        // Kategori değiştiğinde ürün listesini sıfırla
        if (SelectedCategory?.Id != _lastLoadedCategoryId)
        {
            _showProducts = false;
            _products = [];
            _lastLoadedCategoryId = SelectedCategory?.Id;
        }
    }

    private async Task LoadProducts()
    {
        if (SelectedCategory is null) return;

        _loadingProducts = true;
        _showProducts = true;
        StateHasChanged();

        try
        {
            using var dbContext = ContextFactory.CreateDbContext();
            _products = await dbContext.MainProducts
                .AsNoTracking()
                .Where(p => p.CategoryId == SelectedCategory.Id && !p.IsDeleted)
                .SelectMany(p => p.ProductVariants.Where(v => !v.IsDeleted), (p, v) => new CategoryProductItem
                {
                    ProductId = p.Id,
                    Title = p.Title ?? "",
                    Barcode = v.Barcode ?? "",
                    SalePrice = v.SalePrice,
                    Stock = dbContext.BranchOfficeStocks
                        .Where(s => s.ProductVariantId == v.Id)
                        .Sum(s => s.FirstTotalStock - s.SoldQuantity)
                })
                .OrderBy(p => p.Title)
                .Take(50)
                .ToListAsync();
        }
        finally
        {
            _loadingProducts = false;
        }
    }

    private void NavigateToProduct(Guid productId)
    {
        NavigationManager.NavigateTo($"/products/{productId}");
    }

    private string GetParentCategoryName(int parentId)
    {
        return GetParentCategoryNameFunc?.Invoke(parentId) ?? "N/A";
    }

    private class CategoryProductItem
    {
        public Guid ProductId { get; init; }
        public string Title { get; init; } = "";
        public string Barcode { get; init; } = "";
        public decimal SalePrice { get; init; }
        public int Stock { get; init; }
    }
}
