using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.Attributes;

public partial class AttributesPage
{
    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private MudDataGrid<AppCategoryAttribute> _dataGrid = null!;
    private AppCategoryAttribute? _selectedAttribute;
    private string _searchString = string.Empty;

    private async Task<GridData<AppCategoryAttribute>> ServerData(GridState<AppCategoryAttribute> state)
    {
        var result = await AttributeManager.GetCategoryAttributesPageable(
            new SearchablePageDto(_searchString, state.Page, state.PageSize));

        if (result.Success && result.Data is not null)
            return new GridData<AppCategoryAttribute>
            {
                TotalItems = result.Data.TotalItemCount,
                Items = result.Data.Items
            };

        return new GridData<AppCategoryAttribute> { TotalItems = 0, Items = [] };
    }

    private Task OnSearchChanged(string text)
    {
        _searchString = text;
        return _dataGrid.ReloadServerData();
    }

    private void OnAttributeSelected(AppCategoryAttribute attribute)
    {
        _selectedAttribute = attribute;
    }

    private async Task OnAttributeChanged()
    {
        _selectedAttribute = null;
        await _dataGrid.ReloadServerData();
    }
}
