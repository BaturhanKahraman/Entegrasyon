using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Shared.Entity;

namespace Entegrasyon.Blazor.Pages.Brand;

public partial class BrandsPage : ComponentBase
{
    [Inject] private IBrandService brandService { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Parameter] private int PageIndex { get; set; } = 1;
    [Parameter] private int PageSize { get; set; } = 50;
    [Parameter] private string BrandName { get; set; } = string.Empty;
    private IEnumerable<BrandListDetailDto> brandList;
    private string? errorMessage;
    private bool isLoading;

    protected override async Task OnInitializedAsync()
    {
        await LoadData(new GetBrandDetailsPageDto(BrandName, PageIndex, PageSize));
    }

    private async Task LoadData(GetBrandDetailsPageDto dto)
    {
        isLoading = true;
        errorMessage = null;

        var result = await brandService.GetBrandDetailPageable(dto);
        if (result.Success)
        {
            brandList = result.Data;
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
