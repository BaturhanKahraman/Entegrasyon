using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductDetail
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProductService ProductManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private ProductDetailDto? _product;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        var result = await ProductManager.GetProductDetailById(Id);
        if (!result.Success || result.Data is null)
        {
            Snackbar.Add("Ürün bulunamadı.", Severity.Error);
            _loading = false;
            NavigationManager.NavigateTo("/products");
            return;
        }

        _product = result.Data;
        _loading = false;
    }

    private void GoToEdit() => NavigationManager.NavigateTo($"/products/edit/{Id}");
    private void GoBack() => NavigationManager.NavigateTo("/products");
}
