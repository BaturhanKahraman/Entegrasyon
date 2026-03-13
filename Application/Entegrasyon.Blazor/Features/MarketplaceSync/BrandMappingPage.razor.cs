using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingPage : ComponentBase
{
    [Inject]
    public IBrandMatchService BrandMatchService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    // State
    public List<BrandMarketPlaceMatchDto> BrandMappings { get; set; } = [];
    public BrandMappingSummaryDto MappingSummary { get; set; } = new();
    public bool IsLoading { get; set; } = true;
    public int SelectedTabIndex { get; set; } = 0;

    private const int TrendyolMarketPlaceId = 1;

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;
        await LoadMappings();
        await LoadSummary();
        IsLoading = false;
    }

    private async Task LoadMappings()
    {
        BrandMappings = await BrandMatchService.GetAllBrandMappingsAsync(TrendyolMarketPlaceId);
    }

    private async Task LoadSummary()
    {
        MappingSummary = await BrandMatchService.GetBrandMappingsSummaryAsync();
    }

    public async Task OpenMappingDialog()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<BrandMappingDialog>("Brand Mapping Oluştur", null, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await RefreshData();
        }
    }

    public async Task DeleteMapping(BrandMarketPlaceMatchDto mapping)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Mapping'i Sil",
            $"'{mapping.ApplicationBrandName}' brand'ı için mapping'i silmek istediğinize emin misiniz?",
            yesText: "Evet", cancelText: "İptal");

        if (confirmed.HasValue && confirmed.Value)
        {
            try
            {
                var result = await BrandMatchService.RemoveBrandMappingAsync(mapping.ApplicationBrandId, mapping.MarketPlaceId);
                if (result.Success)
                {
                    Snackbar.Add("Mapping başarıyla silindi.", Severity.Success);
                    await RefreshData();
                }
                else
                {
                    Snackbar.Add($"Hata: {result.Message}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Silme işlemi sırasında hata oluştu: {ex.Message}", Severity.Error);
            }
        }
    }

    public async Task ImportBrands()
    {
        try
        {
            var result = await BrandMatchService.ImportTrendyolBrandsAsync();
            if (result.Success)
            {
                Snackbar.Add("Brand'lar başarıyla import edildi.", Severity.Success);
                await RefreshData();
            }
            else
            {
                Snackbar.Add(result.Message, Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Import sırasında hata oluştu: {ex.Message}", Severity.Error);
        }
    }

    private async Task RefreshData()
    {
        IsLoading = true;
        await LoadMappings();
        await LoadSummary();
        IsLoading = false;
    }
}
