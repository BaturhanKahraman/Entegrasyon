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
    private List<CategoryAttribute> _categoryAttributes;
    public EventCallback<List<CategoryAttribute>> OnConfirmedSelectedAttributes { get; set; }

    public async Task GetCategoryAttributes()
    {
        if (_categoryAttributes != null)
            return;
        var result = await _categoryManager.GetCategoryAttributes();
        this._categoryAttributes = result.Data;
    }

    public async Task Open()
    {
        await this.GetCategoryAttributes();
        _modalRef.Show();
    }
    private void Close() => _modalRef.Hide();


}
