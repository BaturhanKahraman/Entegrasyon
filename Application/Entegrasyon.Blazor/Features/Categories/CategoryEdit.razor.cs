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
    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private ICategoryAttributeCategoryManager AttributeCategoryManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IProductService ProductManager { get; set; } = null!;

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
            await LoadCategory();

            if (_category != null)
            {
                // All independent — run in parallel
                await Task.WhenAll(
                    LoadCategories(),
                    LoadAllAttributes(),
                    LoadCategoryAttributes(),
                    LoadProductStatus());
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadCategory()
    {
        try
        {
            var result = await CategoryManager.GetCategoryEditDetail(Id);
            if (result.Success && result.Data is not null)
            {
                _category = result.Data;
                _isLeafCategory = !await CategoryManager.IsSuper(Id);

                _model = new CategoryFormModel
                {
                    Id = _category.Id,
                    Name = _category.Name,
                    IsFavorite = _category.IsFavorite,
                    ParentCategoryId = _category.SuperCategoryId
                };
            }
            else
            {
                Snackbar.Add("Kategori bulunamadı", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Kategori yüklenirken hata: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadProductStatus()
    {
        _hasSoldProducts = await ProductManager.HasSoldProductsInCategory(Id);
        if (!_hasSoldProducts)
            _hasProducts = await ProductManager.GetProductCountByCategoryId(Id) > 0;
        else
            _hasProducts = true; // sold products implies products exist
    }

    private async Task LoadCategoryAttributes()
    {
        try
        {
            var result = await AttributeManager.GetCategoryAttributesByCategory(Id);
            if (result.Success && result.Data is not null)
            {
                _model.Attributes = result.Data.Select(a => new AttributeModel
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
            Snackbar.Add($"Kategori özellikleri yüklenirken hata: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadCategories()
    {
        try
        {
            _availableCategories = await CategoryManager.GetValidParentCandidatesAsync(Id);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Kategoriler yüklenirken hata: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadAllAttributes()
    {
        try
        {
            var result = await AttributeManager.GetCategoryAttributes();
            if (result.Success && result.Data is not null)
                _allAttributes = result.Data;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Özellikler yüklenirken hata: {ex.Message}", Severity.Error);
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
