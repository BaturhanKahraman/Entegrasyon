using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryEdit
{
    [Parameter] public int Id { get; set; }

    [Inject] private ICategoryService CategoryManager { get; set; } = null!;
    [Inject] private ICategoryAttributeCategoryManager AttributeCategoryManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private MudForm _form = null!;
    private bool _isValid;
    private bool _saving;
    private bool _loading = true;
    private Category? _category;
    private CategoryFormModel _model = new();
    private List<Category> _availableCategories = [];
    private List<AppCategoryAttribute> _allAttributes = [];
    private bool _isLeafCategory = true;
    private bool _hasSoldProducts;
    private bool _hasProducts;

    private int _tempIdCounter = -1;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        try
        {
            var result = await CategoryManager.GetCategoryEditPageData(Id);
            if (!result.Success || result.Data is null)
            {
                Snackbar.Add(result.Message ?? "Kategori bulunamadı", Severity.Error);
                return;
            }

            var data = result.Data;
            _category = data.Category;
            _isLeafCategory = data.IsLeaf;
            _availableCategories = data.ValidParentCandidates;
            _allAttributes = data.AllAttributes;
            _hasSoldProducts = data.HasSoldProducts;
            _hasProducts = data.HasProducts;

            _model = new CategoryFormModel
            {
                Id = _category.Id,
                Name = _category.Name,
                IsFavorite = _category.IsFavorite,
                ParentCategoryId = _category.SuperCategoryId
            };

            if (data.CategoryAttributes.Count > 0)
            {
                _model.Attributes = data.CategoryAttributes.Select(a => new AttributeModel
                {
                    ExistingAttributeId = a.Id,
                    Name = a.CategoriyAttributeHumanized,
                    IsRequired = a.IsRequired,
                    IsVarianter = a.IsVarianter,
                    IsSlicer = a.IsSlicer,
                    AllowCustom = a.AllowCustom,
                    IsExisting = true,
                    Values = a.CategoryAttributeValues?.ToList() ?? [],
                    SelectedAttribute = _allAttributes.FirstOrDefault(x => x.Id == a.Id)
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Sayfa yüklenirken hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void AddNewAttribute()
    {
        _model.Attributes.Add(new AttributeModel { IsExisting = false });
    }

    private void AddExistingAttribute()
    {
        _model.Attributes.Add(new AttributeModel { IsExisting = true });
    }

    private void RemoveAttribute(int index)
    {
        _model.Attributes.RemoveAt(index);
    }

    private void OnExistingAttributeSelected(AppCategoryAttribute? attr, AttributeModel model)
    {
        if (attr is null) return;
        model.SelectedAttribute = attr;
        model.ExistingAttributeId = attr.Id;
        model.Name = attr.CategoryAttributeHumanized;
        model.AllowCustom = attr.AllowCustom;
        model.Values = attr.CategoryAttributeValues?.ToList() ?? [];
    }

    private async Task OnVarianterChanged(bool value, AttributeModel model)
    {
        if (value != model.IsVarianter)
        {
            var confirm = await DialogService.ShowMessageBox(
                "Uyarı", "Bu değişiklik mevcut ürünleri etkileyebilir. Devam etmek istiyor musunuz?",
                yesText: "Evet", cancelText: "İptal");
            if (confirm != true) return;
        }

        if (value)
        {
            foreach (var a in _model.Attributes.Where(a => a != model))
                a.IsVarianter = false;
            model.IsSlicer = false;
        }
        model.IsVarianter = value;
    }

    private async Task OnSlicerChanged(bool value, AttributeModel model)
    {
        if (value != model.IsSlicer)
        {
            var confirm = await DialogService.ShowMessageBox(
                "Uyarı", "Bu değişiklik mevcut ürünleri etkileyebilir. Devam etmek istiyor musunuz?",
                yesText: "Evet", cancelText: "İptal");
            if (confirm != true) return;
        }

        if (value)
        {
            foreach (var a in _model.Attributes.Where(a => a != model))
                a.IsSlicer = false;
            model.IsVarianter = false;
        }
        model.IsSlicer = value;
    }

    private Task<IEnumerable<AppCategoryAttribute>> SearchAttributes(string value, CancellationToken ct)
    {
        var usedIds = _model.Attributes
            .Where(a => a.IsExisting && a.ExistingAttributeId > 0)
            .Select(a => a.ExistingAttributeId)
            .ToHashSet();

        if (string.IsNullOrEmpty(value))
            return Task.FromResult(_allAttributes.Where(a => !usedIds.Contains(a.Id)));

        return Task.FromResult(_allAttributes
            .Where(a => !usedIds.Contains(a.Id) &&
                        a.CategoryAttributeHumanized.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    private Task AddValue(AttributeModel attr)
    {
        if (string.IsNullOrWhiteSpace(attr.NewValueName)) return Task.CompletedTask;

        var newValue = new CategoryAttributeValue
        {
            Id = _tempIdCounter--,
            Name = attr.NewValueName.Trim(),
            CategoryAttributeId = attr.ExistingAttributeId
        };

        attr.Values.Add(newValue);
        attr.NewValueName = string.Empty;
        return Task.CompletedTask;
    }

    private void RemoveValue(AttributeModel attr, CategoryAttributeValue value)
    {
        attr.Values.Remove(value);
    }

    private async Task Save()
    {
        await _form.Validate();
        if (!_isValid) return;

        _saving = true;
        try
        {
            var editDto = new EditCategoryDto(
                Id: _model.Id,
                Name: _model.Name,
                SuperCategoryId: _model.ParentCategoryId,
                IsFavorite: _model.IsFavorite,
                IsImported: _category?.IsImported ?? false
            );

            var result = await CategoryManager.UpdateCategory(editDto);
            if (!result.Success)
            {
                Snackbar.Add(result.Message ?? "Kategori güncellenirken hata oluştu", Severity.Error);
                return;
            }

            if (_isLeafCategory)
            {
                var attrDtos = _model.Attributes.Select(ToAddCategoryAttributeDto);
                var attrResult = await AttributeCategoryManager.AddCategoryAttributeForCategory(
                    _model.Id, attrDtos);

                if (!attrResult.Success)
                {
                    Snackbar.Add($"Kategori güncellendi fakat özellikler kaydedilemedi: {attrResult.Message}", Severity.Warning);
                    NavigationManager.NavigateTo("/categories");
                    return;
                }
            }

            Snackbar.Add("Kategori güncellendi", Severity.Success);
            NavigationManager.NavigateTo("/categories");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private static AddCategoryAttributeDto ToAddCategoryAttributeDto(AttributeModel a) => new(
        id: a.IsExisting ? a.ExistingAttributeId : 0,
        isRequired: a.IsRequired,
        allowCustom: a.AllowCustom,
        isVarianter: a.IsVarianter,
        categoryAttributeKey: a.IsExisting
            ? (a.SelectedAttribute?.CategoryAttributeKey ?? a.Name.ToLower().Replace(" ", "_"))
            : a.Name.ToLower().Replace(" ", "_"),
        isSlicer: a.IsSlicer,
        categoryAttributeHumanized: a.Name,
        categoryAttributeValues: a.Values
    );

    private void GoBack() => NavigationManager.NavigateTo("/categories");

    private class CategoryFormModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public int? ParentCategoryId { get; set; }
        public List<AttributeModel> Attributes { get; set; } = [];
    }

    private class AttributeModel
    {
        public bool IsExisting { get; set; }
        public int ExistingAttributeId { get; set; }
        public AppCategoryAttribute? SelectedAttribute { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsVarianter { get; set; }
        public bool IsSlicer { get; set; }
        public bool AllowCustom { get; set; } = true;
        public List<CategoryAttributeValue> Values { get; set; } = [];
        public string NewValueName { get; set; } = string.Empty;
    }
}
