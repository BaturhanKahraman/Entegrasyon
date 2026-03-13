using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    public IBrandService BrandService { get; set; } = null!;

    [Inject]
    public IBrandMatchService BrandMatchService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    // Form State
    public CreateBrandMarketPlaceMatchDto FormDto { get; set; } = new();
    public List<BrandDto> AllBrands { get; set; } = [];
    public bool IsLoading { get; set; } = true;
    public bool IsSubmitting { get; set; } = false;
    public BrandDto? SelectedBrand { get; set; }

    private MudForm _form = null!;
    private const int TrendyolMarketPlaceId = 1;

    protected override async Task OnInitializedAsync()
    {
        FormDto.MarketPlaceId = TrendyolMarketPlaceId;
        await LoadBrands();
    }

    private async Task LoadBrands()
    {
        IsLoading = true;
        var result = await BrandService.GetBrandListDetails();
        if (result.Success)
        {
            AllBrands = result.Data?
                .Select(x => new BrandDto { Id = x.Id, Name = x.Name })
                .OrderBy(x => x.Name)
                .ToList() ?? [];
        }
        else
        {
            Snackbar.Add("Brand'lar yüklenirken hata oluştu.", Severity.Error);
        }
        IsLoading = false;
    }

    public async Task SubmitForm()
    {
        try
        {
            if (_form == null) return;

            await _form.Validate();
            if (!_form.IsValid) return;

            // Validate Brand selection
            if (FormDto.ApplicationBrandId <= 0)
            {
                Snackbar.Add("Lütfen bir brand seçiniz.", Severity.Error);
                return;
            }

            IsSubmitting = true;

            var result = await BrandMatchService.CreateBrandMappingAsync(FormDto);
            if (result.Success)
            {
                Snackbar.Add("Marka eşleştirme başarıylaoluşturuldu.", Severity.Success);
                MudDialog.Close(DialogResult.Ok(FormDto));
            }
            else
            {
                Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Form gönderimi sırasında hata oluştu: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public void HandleBrandSelection(BrandDto? brand)
    {
        if (brand != null)
        {
            FormDto.ApplicationBrandId = brand.Id;
            SelectedBrand = brand;
        }
    }

    public void Cancel()
    {
        MudDialog.Cancel();
    }
}
