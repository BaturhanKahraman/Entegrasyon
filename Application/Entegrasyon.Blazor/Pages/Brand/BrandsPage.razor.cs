using Entegrasyon.Business.Abstract;
using Entegrasyon.Blazor.Components.Dialogs;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Pages.Brand;

public partial class BrandsPage : ComponentBase
{
    [Inject] private IBrandService BrandService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private List<BrandListDetailDto> _brands = [];
    private bool _loading;
    private string _searchTerm = string.Empty;

    private Func<BrandListDetailDto, bool> QuickFilter => brand =>
        string.IsNullOrWhiteSpace(_searchTerm) ||
        brand.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        await LoadBrandsAsync();
    }

    private async Task LoadBrandsAsync()
    {
        _loading = true;
        var result = await BrandService.GetBrandListDetails();
        _loading = false;

        if (result.Success)
            _brands = result.Data ?? [];
        else
            Snackbar.Add(result.Message ?? "Markalar yüklenemedi.", Severity.Error);
    }

    private async Task OpenAddDialog()
    {
        var dialog = await DialogService.ShowAsync<BrandDialog>("Marka Ekle");
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadBrandsAsync();
    }

    private async Task OpenEditDialog(BrandListDetailDto brand)
    {
        var parameters = new DialogParameters<BrandDialog>
        {
            { x => x.IsEditMode, true },
            { x => x.Brand, brand }
        };
        var dialog = await DialogService.ShowAsync<BrandDialog>("Marka Düzenle", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadBrandsAsync();
    }

    private async Task DeleteAsync(BrandListDetailDto brand)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Marka Sil",
            $"\"{brand.Name}\" markasını silmek istediğinizden emin misiniz?",
            yesText: "Sil",
            cancelText: "İptal");

        if (confirmed != true) return;

        var result = await BrandService.DeleteBrand(brand.Id);
        if (result.Success)
        {
            Snackbar.Add("Marka silindi.", Severity.Success);
            await LoadBrandsAsync();
        }
        else
        {
            Snackbar.Add(result.Message ?? "Marka silinirken hata oluştu.", Severity.Error);
        }
    }
}
