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
    private List<CategoryAttribute> _categoryAttributes = new();
    private List<CategoryAttribute> _selectedCatAttributes = new();
    private string _selectedCategoryAttributesAsString=string.Empty;
    public EventCallback<List<CategoryAttribute>> OnConfirmedSelectedAttributes { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await GetCategoryAttributes();
        await base.OnInitializedAsync();
    }
    public async Task GetCategoryAttributes()
    {
        if (!_categoryAttributes.Any())
        {
            var result = await _categoryManager.GetCategoryAttributes();
            _categoryAttributes = result.Data;
        }        
    }

    public async Task Open()
    {
        _modalRef.Show();
    }
    private void Close() => _modalRef.Hide();

    private void WriteSelected()
    {
        Console.WriteLine(_selectedCategoryAttributesAsString);
        //foreach (var item in _selectedCatAttributes)
        //{
        //    Console.WriteLine(item.CategoryAttributeKey);
        //}
    }

}
