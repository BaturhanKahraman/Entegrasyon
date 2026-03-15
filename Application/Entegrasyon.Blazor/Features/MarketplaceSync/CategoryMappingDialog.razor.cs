using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class CategoryMappingDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject] private ICategoryService CategoryService { get; set; } = null!;
    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private CreateCategoryMarketplaceMatchDto _formDto = new();
    private List<Category> _allCategories = [];
    private Category? _selectedCategory;
    private MudForm _form = null!;
    private bool _isLoading = true;
    private bool _isSubmitting;

    private const int TrendyolMarketPlaceId = 1;

    protected override async Task OnInitializedAsync()
    {
        _formDto.MarketPlaceId = TrendyolMarketPlaceId;
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        _isLoading = true;
        _allCategories = await CategoryService.GetAllCategoriesWithoutAttributesAsync();
        _isLoading = false;
    }

    private async Task SubmitForm()
    {
        try
        {
            if (_form is null) return;

            await _form.Validate();
            if (!_form.IsValid) return;

            if (_selectedCategory is not null)
                _formDto.ApplicationCategoryId = _selectedCategory.Id;

            if (_formDto.ApplicationCategoryId <= 0)
            {
                Snackbar.Add("Lütfen bir kategori seçiniz.", Severity.Error);
                return;
            }

            _isSubmitting = true;

            var result = await CategoryMatchService.CreateCategoryMappingAsync(_formDto);
            if (result.Success)
            {
                Snackbar.Add("Kategori eşleştirme başarıyla oluşturuldu.", Severity.Success);
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
