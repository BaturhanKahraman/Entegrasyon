using Entegrasyon.Blazor.Components.Shared;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizard
{
    [Parameter] public int? Id { get; set; }

    [Inject] private ICategoryService CategoryService { get; set; } = null!;
    [Inject] private ICategoryAttributeCategoryManager CategoryAttributeManager { get; set; } = null!;
    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private bool IsEditMode => Id is > 0;

    // Step index
    private int _activeStep;

    // Step component refs
    private CategoryWizardGeneralStep _generalStep = null!;
    private CategoryWizardAttributesStep _attributesStep = null!;
    private CategoryWizardMarketplaceStep _marketplaceStep = null!;

    // General step state
    private string _name = string.Empty;
    private int? _parentCategoryId;
    private bool _isFavorite;
    private decimal? _defaultVatRate;

    // Attributes step state
    private List<AttributeModel> _attributes = [];

    // Marketplace step state
    private int? _selectedMarketplaceId;

    // Saved category ID (set after save so marketplace step can use it)
    private int? _savedCategoryId;

    // UI state
    private bool _isDirty;
    private bool _loading;
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode)
        {
            await LoadCategoryData();
        }
    }

    private async Task LoadCategoryData()
    {
        _loading = true;
        try
        {
            var result = await CategoryService.GetCategoryEditPageData(Id!.Value);
            if (!result.Success || result.Data is null)
            {
                Snackbar.Add(result.Message ?? "Kategori yüklenemedi.", Severity.Error);
                return;
            }

            var data = result.Data;
            var category = data.Category;

            _name = category.Name ?? string.Empty;
            _parentCategoryId = category.SuperCategoryId;
            _isFavorite = category.IsFavorite;
            _defaultVatRate = category.DefaultVatRate;
            _savedCategoryId = category.Id;

            // Map CategoryAttributeDto list → AttributeModel list
            _attributes = data.CategoryAttributes.Select(a => new AttributeModel
            {
                Id = a.Id,
                IsExisting = true,
                CategoryAttributeKey = a.CategoryAttributeKey ?? string.Empty,
                CategoryAttributeHumanized = a.CategoriyAttributeHumanized ?? string.Empty,
                AllowCustom = a.AllowCustom,
                IsRequired = a.IsRequired,
                IsVarianter = a.IsVarianter,
                IsSlicer = a.IsSlicer,
                Values = a.CategoryAttributeValues
                    .Select(v => new ValueModel { Id = v.Id, Value = v.Name ?? string.Empty })
                    .ToList(),
                SelectedAttribute = data.AllAttributes.FirstOrDefault(x => x.Id == a.Id)
            }).ToList();
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task NextStep()
    {
        if (_activeStep == 0)
        {
            var valid = await _generalStep.ValidateAsync();
            if (!valid)
            {
                Snackbar.Add("Lütfen gerekli alanları doldurun.", Severity.Warning);
                return;
            }
        }
        else if (_activeStep == 1)
        {
            var error = _attributesStep.GetValidationError();
            if (error is not null)
            {
                Snackbar.Add(error, Severity.Warning);
                return;
            }
        }

        _activeStep++;
    }

    private void PreviousStep()
    {
        if (_activeStep > 0)
            _activeStep--;
    }

    private async Task Save()
    {
        _saving = true;
        try
        {
            // Validate current step (attributes)
            var error = _attributesStep.GetValidationError();
            if (error is not null)
            {
                Snackbar.Add(error, Severity.Warning);
                return;
            }

            int categoryId;

            if (IsEditMode)
            {
                categoryId = Id!.Value;

                var editDto = new EditCategoryDto(
                    Id: categoryId,
                    Name: _name,
                    SuperCategoryId: _parentCategoryId,
                    IsFavorite: _isFavorite,
                    IsImported: false,
                    DefaultVatRate: _defaultVatRate
                );

                var updateResult = await CategoryService.UpdateCategory(editDto);
                if (!updateResult.Success)
                {
                    Snackbar.Add(updateResult.Message ?? "Kategori güncellenemedi.", Severity.Error);
                    return;
                }
            }
            else
            {
                var attributeDtos = BuildAttributeDtos();

                var addDto = new AddCategoryDto(
                    Name: _name,
                    CategoryAttributes: attributeDtos,
                    SuperCategoryId: _parentCategoryId,
                    IsFavorite: _isFavorite,
                    DefaultVatRate: _defaultVatRate
                );

                var addResult = await CategoryService.AddCategory(addDto);
                if (!addResult.Success || addResult.Data is null)
                {
                    Snackbar.Add(addResult.Message ?? "Kategori eklenemedi.", Severity.Error);
                    return;
                }

                categoryId = addResult.Data.Id;
                _savedCategoryId = categoryId;
            }

            // In edit mode, save attributes separately
            if (IsEditMode)
            {
                var attributeDtos = BuildAttributeDtos();
                var attrResult = await CategoryAttributeManager.AddCategoryAttributeForCategory(categoryId, attributeDtos);
                if (!attrResult.Success)
                {
                    Snackbar.Add(attrResult.Message ?? "Özellikler kaydedilemedi.", Severity.Warning);
                }
            }

            // Save marketplace mapping if selected
            var mappingDto = _marketplaceStep?.GetMappingDto();
            if (mappingDto is not null)
            {
                mappingDto = mappingDto with { ApplicationCategoryId = categoryId };
                var mappingResult = await CategoryMatchService.CreateCategoryMappingAsync(mappingDto);
                if (!mappingResult.Success)
                {
                    Snackbar.Add(mappingResult.Message ?? "Pazar yeri eşleştirmesi kaydedilemedi.", Severity.Warning);
                }
            }

            _isDirty = false;
            Snackbar.Add(IsEditMode ? "Kategori güncellendi." : "Kategori eklendi.", Severity.Success);

            await NavigateAfterSave(categoryId);
        }
        finally
        {
            _saving = false;
        }
    }

    private List<AddCategoryAttributeDto> BuildAttributeDtos()
    {
        return _attributes.Select(a => new AddCategoryAttributeDto
        {
            Id = a.Id > 0 ? a.Id : 0,
            IsRequired = a.IsRequired,
            AllowCustom = a.AllowCustom,
            IsVarianter = a.IsVarianter,
            IsSlicer = a.IsSlicer,
            CategoryAttributeKey = a.CategoryAttributeKey,
            CategoryAttributeHumanized = a.CategoryAttributeHumanized,
            CategoryAttributeValues = a.Values
                .Select(v => new CategoryAttributeValue { Id = v.Id > 0 ? v.Id : 0, Name = v.Value })
                .ToList()
        }).ToList();
    }

    private async Task NavigateAfterSave(int categoryId)
    {
        if (_selectedMarketplaceId.HasValue && _marketplaceStep?.HasMapping == true)
        {
            var parameters = new DialogParameters<ConfirmDialog<bool>>
            {
                { x => x.Message, "Özellik eşleştirmesine geçmek ister misiniz?" },
                { x => x.ConfirmText, "Evet" },
                { x => x.ConfirmColor, Color.Primary },
                { x => x.Icon, Icons.Material.Filled.CompareArrows },
                { x => x.IconColor, Color.Info }
            };

            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall };
            var dialog = await DialogService.ShowAsync<ConfirmDialog<bool>>("Özellik Eşleştirme", parameters, options);
            var result = await dialog.Result;

            if (result is not null && !result.Canceled)
            {
                NavigationManager.NavigateTo($"/attributes?categoryId={categoryId}&marketplaceId={_selectedMarketplaceId.Value}");
                return;
            }
        }

        NavigationManager.NavigateTo("/categories");
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/categories");
    }

    private void MarkDirty()
    {
        _isDirty = true;
    }
}
