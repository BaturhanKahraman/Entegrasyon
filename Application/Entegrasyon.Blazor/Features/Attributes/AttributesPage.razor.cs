using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.Attributes;

public partial class AttributesPage
{
    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<AppCategoryAttribute> _attributes = [];
    private AppCategoryAttribute? _selectedAttribute;
    private bool _loading = true;
    private string _searchString = string.Empty;

    private IEnumerable<AppCategoryAttribute> FilteredAttributes =>
        string.IsNullOrWhiteSpace(_searchString)
            ? _attributes
            : _attributes.Where(a =>
                a.CategoryAttributeHumanized.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
                a.CategoryAttributeKey.Contains(_searchString, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadAttributes();
    }

    private async Task LoadAttributes()
    {
        _loading = true;
        try
        {
            var result = await AttributeManager.GetCategoryAttributes();
            if (result.Success && result.Data != null)
                _attributes = result.Data;
            else
                Snackbar.Add(result.Message ?? "Özellikler yüklenemedi", Severity.Warning);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Özellikler yüklenirken hata oluştu: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnAttributeSelected(AppCategoryAttribute attribute)
    {
        _selectedAttribute = attribute;
    }

    private async Task OnAttributeChanged()
    {
        _selectedAttribute = null;
        await LoadAttributes();
    }
}
