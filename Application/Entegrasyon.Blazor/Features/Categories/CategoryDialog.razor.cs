using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public Category? Category { get; set; }

    [Parameter]
    public bool IsEditMode { get; set; }

    [Inject] private ICategoryService CategoryManager { get; set; } = null!;
    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private ICategoryAttributeCategoryManager AttributeCategoryManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private MudForm _form = null!;
    private bool _isValid;
    private bool _saving;
    private CategoryFormModel _model = new();
    private List<Category> _availableCategories = [];
    private List<AppCategoryAttribute> _allAttributes = [];
    private bool _isLeafCategory = true;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadCategories(), LoadAllAttributes());

        if (IsEditMode && Category != null)
        {
            _isLeafCategory = Category.SubCategories == null || !Category.SubCategories.Any();

            _model = new CategoryFormModel
            {
                Id = Category.Id,
                Name = Category.Name,
                IsFavorite = Category.IsFavorite,
                ParentCategoryId = Category.SuperCategoryId,
                Attributes = Category.CategoryAttributes?.Select(a => new AttributeModel
                {
                    ExistingAttributeId = a.CategoryAttributeId,
                    SelectedAttribute = a.CategoryAttribute,
                    Name = a.CategoryAttribute?.CategoryAttributeHumanized ?? string.Empty,
                    IsRequired = a.IsRequired,
                    IsVarianter = a.IsVarianter,
                    IsSlicer = a.IsSlicer,
                    AllowCustom = a.CategoryAttribute?.AllowCustom ?? true,
                    IsExisting = true
                }).ToList() ?? []
            };
        }
    }

    private async Task LoadCategories()
    {
        try
        {
            _availableCategories = await CategoryManager.GetValidParentCandidatesAsync(
                IsEditMode ? Category?.Id : null);
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
    }

    private async Task OnVarianterChanged(bool value, AttributeModel model)
    {
        if (IsEditMode && value != model.IsVarianter)
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
        if (IsEditMode && value != model.IsSlicer)
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

    private async Task Submit()
    {
        await _form.Validate();
        if (!_isValid) return;

        _saving = true;
        try
        {
            if (IsEditMode)
                await SubmitEdit();
            else
                await SubmitAdd();
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

    private async Task SubmitAdd()
    {
        var addDto = new AddCategoryDto(
            Name: _model.Name,
            CategoryAttributes: [],
            SuperCategoryId: _model.ParentCategoryId,
            IsFavorite: _model.IsFavorite
        );

        var result = await CategoryManager.AddCategory(addDto);
        if (!result.Success)
        {
            Snackbar.Add(result.Message ?? "Kategori eklenirken hata oluştu", Severity.Error);
            return;
        }

        if (_model.Attributes.Count > 0 && result.Data is not null)
        {
            var attrDtos = _model.Attributes.Select(ToAddCategoryAttributeDto);
            var attrResult = await AttributeCategoryManager.AddCategoryAttributeForCategory(
                result.Data.Id, attrDtos);

            if (!attrResult.Success)
            {
                Snackbar.Add($"Kategori eklendi fakat özellikler kaydedilemedi: {attrResult.Message}", Severity.Warning);
                MudDialog.Close(DialogResult.Ok(true));
                return;
            }
        }

        Snackbar.Add("Kategori eklendi", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task SubmitEdit()
    {
        var editDto = new EditCategoryDto(
            Id: _model.Id,
            Name: _model.Name,
            SuperCategoryId: _model.ParentCategoryId,
            IsFavorite: _model.IsFavorite,
            IsImported: false
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
                MudDialog.Close(DialogResult.Ok(true));
                return;
            }
        }

        Snackbar.Add("Kategori güncellendi", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
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
        categoryAttributeValues: a.SelectedAttribute?.CategoryAttributeValues?.ToList() ?? []
    );

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

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
    }
}
