using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.MVC.Components.Razor.Common.Modal;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.MVC.Components.Razor.CategoryAttributeComponents;

public partial class SelectExistingAttributeModal
{
    private Modal _modalRef;
    [Inject]
    private CategoryAttributeManager _categoryManager { get; set; }
    [Parameter]
    public IEnumerable<int> ExceptIds { get; set; }
    [Parameter]
    public EventCallback<List<CategoryAttribute>> OnPressOk { get; set; }
    private List<CategoryAttribute> _categoryAttributes = new();
    public  CategoryAttribute[] _selectedCatAttributes { get; set; } = new CategoryAttribute[] {};
    private string[] _selectedCategoryAttributesAsString { get; set; }=new string[] {};
    public EventCallback<List<CategoryAttribute>> OnConfirmedSelectedAttributes { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await GetCategoryAttributes();
    }
    protected override void OnParametersSet()
    {
        _categoryAttributes = _categoryAttributes.Where(x => !ExceptIds.Contains(x.Id)).ToList();
    }
    public async Task GetCategoryAttributes()
    {
        if (!_categoryAttributes.Any())
        {
            var result = await _categoryManager.GetCategoryAttributes();
            _categoryAttributes = result.Data;
        }
    }

    public void Open()=> _modalRef.Show();
    
    private void Close() => _modalRef.Hide();

    //private void WriteSelected()
    //{
    //    foreach (var item in _selectedCategoryAttributesAsString)
    //    {
    //        Console.WriteLine()
    //    }
    //}
    private void OnAccepted()
    {
        int[] selectedIds = _selectedCategoryAttributesAsString.Select(x => Convert.ToInt32(x)).ToArray();
        OnPressOk.InvokeAsync(_categoryAttributes.Where(x => selectedIds.Contains(x.Id)).ToList());
    }

}
