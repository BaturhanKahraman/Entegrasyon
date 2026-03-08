using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Requests;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Shared.Entity;

namespace Entegrasyon.Blazor.Pages.Brand;

public partial class BrandsPage : ComponentBase
{
    [Inject] private IBrandService brandService { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Parameter] public int PageIndex { get; set; } = 0;
    [Parameter] public int PageSize { get; set; } = 50;
    [Parameter] public string BrandName { get; set; } = string.Empty;
    private IEnumerable<BrandListDetailDto> brandList;
    private string? errorMessage;
    private bool isLoading;

    protected override async Task OnInitializedAsync()
    {
        await LoadData(new BrandDetailPaginatedRequest { SearchTerm = BrandName });
    }

    private async Task LoadData(BrandDetailPaginatedRequest request)
    {
        isLoading = true;
        errorMessage = null;

        var result = await brandService.GetBrandDetailPageable(request);
        if (result.Success)
        {
            brandList = result.Data.Items;
            isLoading = false;
            return;
        }

        errorMessage = string.IsNullOrWhiteSpace(result.Message)
            ? "Markaları yüklenemedi"
            : result.Message;
        Snackbar.Add(errorMessage, Severity.Error);
        brandList = null!;
        isLoading = false;
    }
}
