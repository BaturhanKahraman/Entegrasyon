namespace Entegrasyon.Blazor.Components.Dialogs;

using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

public partial class CategoryDialog
{
    [CascadingParameter]
    public IMudDialogInstance  MudDialog { get; set; }

    [Parameter]
    public Category? Category { get; set; }

    [Parameter]
    public bool IsEditMode { get; set; }

    [Inject]
    private CategoryManager? CategoryManager { get; set; }

    [Inject]
    private ISnackbar? Snackbar { get; set; }

    private MudForm _form = null!;
    private bool _isValid;
    private bool _saving;
    private CategoryFormModel _model = new();
    private List<Category> _availableCategories = new();
    private bool _isLeafCategory = true; // Yeni kategoriler için varsayılan true

    protected override async Task OnInitializedAsync()
    {
        await LoadCategories();

        if (IsEditMode && Category != null)
        {
            // Kategorinin alt kategorisi var mı kontrol et
            _isLeafCategory = Category.SubCategories == null || !Category.SubCategories.Any();

            _model = new CategoryFormModel
            {
                Id = Category.Id,
                Name = Category.Name,
                IsFavorite = Category.IsFavorite,
                ParentCategoryId = Category.SuperCategoryId,
                Attributes = Category.CategoryAttributes?.Select(a => new AttributeModel
                {
                    Name = a.CategoryAttribute?.CategoryAttributeHumanized ?? string.Empty,
                    Type = "Text",
                    IsRequired = false
                }).ToList() ?? new List<AttributeModel>()
            };
        }
        else
        {
            // Yeni kategori eklerken, varsayılan olarak en alt seviye kabul et
            _isLeafCategory = true;
        }
    }

    private async Task LoadCategories()
    {
        try
        {
            var allCategories = await CategoryManager!.GetAllCategoriesWithoutAttributesAsync();

            // Exclude current category if editing to prevent circular references
            if (IsEditMode && Category?.Id > 0)
            {
                _availableCategories = allCategories
                    .Where(c => c.Id != Category.Id)
                    .ToList();
            }
            else
            {
                _availableCategories = allCategories;
            }
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Kategoriler yüklenirken hata: {ex.Message}", Severity.Error);
        }
    }

    private void AddAttribute()
    {
        _model.Attributes.Add(new AttributeModel
        {
            Name = string.Empty,
            Type = "Text",
            IsRequired = false
        });
    }

    private void RemoveAttribute(int index)
    {
        _model.Attributes.RemoveAt(index);
    }

    private async Task Submit()
    {
        await _form.Validate();
        if (!_isValid)
            return;

        _saving = true;
        try
        {
            if (IsEditMode)
            {
                // Edit mode - update existing category
                var editDto = new EditCategoryDto(
                    Id: _model.Id,
                    Name: _model.Name,
                    SuperCategoryId: _model.ParentCategoryId,
                    IsFavorite: _model.IsFavorite,
                    IsImported: false
                );

                var result = await CategoryManager!.UpdateCategory(editDto);
                if (result.Success)
                {
                    Snackbar?.Add("Kategori güncellendi", Severity.Success);
                    MudDialog?.Close(DialogResult.Ok(true));
                    StateHasChanged();
                }
                else
                {
                    Snackbar?.Add(
                        result.Message ?? "Kategori güncellenirken hata oluştu",
                        Severity.Error
                    );
                }
            }
            else
            {
                // Add mode - create new category
                var addDto = new AddCategoryDto(
                    Name: _model.Name,
                    CategoryAttributes: _model.Attributes.Select(a =>
                        new AddCategoryAttributeDto(
                            0,
                            a.IsRequired,
                            true,
                            false,
                            a.Name.ToLower().Replace(" ", "_"),
                            false,
                            a.Name,
                            new List<CategoryAttributeValue>()
                        )
                    ),
                    SuperCategoryId: _model.ParentCategoryId,
                    IsFavorite: _model.IsFavorite
                );

                var result = await CategoryManager!.AddCategory(addDto);
                if (result.Success)
                {
                    Snackbar?.Add("Kategori eklendi", Severity.Success);
                    MudDialog?.Close(DialogResult.Ok(true));
                    StateHasChanged();
                }
                else
                {
                    Snackbar?.Add(
                        result.Message ?? "Kategori eklenirken hata oluştu",
                        Severity.Error
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());

    private class CategoryFormModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public int? ParentCategoryId { get; set; }
        public List<AttributeModel> Attributes { get; set; } = new();
    }

    private class AttributeModel
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Text";
        public bool IsRequired { get; set; }
    }
}
