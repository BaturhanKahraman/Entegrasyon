using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class AttributeMappingDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private CreateAttributeMarketPlaceMatchDto _formDto = new();
    private List<AppCategoryAttribute> _allAttributes = [];
    private AppCategoryAttribute? _selectedAttribute;
    private MudForm _form = null!;
    private bool _isLoading = true;
    private bool _isSubmitting;

    private const int TrendyolMarketPlaceId = 1;

    protected override async Task OnInitializedAsync()
    {
        _formDto.MarketPlaceId = TrendyolMarketPlaceId;
        await LoadAttributes();
    }

    private async Task LoadAttributes()
    {
        _isLoading = true;
        var result = await AttributeManager.GetCategoryAttributes();
        if (result.Success && result.Data is not null)
            _allAttributes = result.Data.OrderBy(x => x.CategoryAttributeHumanized).ToList();
        else
            Snackbar.Add("Özellikler yüklenirken hata oluştu.", Severity.Error);
        _isLoading = false;
    }

    private async Task SubmitForm()
    {
        try
        {
            if (_form is null) return;

            await _form.Validate();
            if (!_form.IsValid) return;

            if (_selectedAttribute is not null)
                _formDto.ApplicationCategoryAttributeId = _selectedAttribute.Id;

            if (_formDto.ApplicationCategoryAttributeId <= 0)
            {
                Snackbar.Add("Lütfen bir özellik seçiniz.", Severity.Error);
                return;
            }

            _isSubmitting = true;

            var result = await AttributeManager.CreateAttributeMarketPlaceMatchAsync(_formDto);
            if (result.Success)
            {
                Snackbar.Add("Özellik eşleştirme başarıyla oluşturuldu.", Severity.Success);
                MudDialog.Close(DialogResult.Ok(_formDto));
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
            _isSubmitting = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
