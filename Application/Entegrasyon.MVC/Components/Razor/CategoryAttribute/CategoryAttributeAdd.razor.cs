using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.MVC.Components.Razor.CategoryAttribute
{
    public partial class CategoryAttributeAdd
    {
        private CategoryAttributeAddViewModel _model;
        [Parameter]
        public string CatId { get; set; }
        [Parameter]
        public string CategoryName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _model ??= new();
            await base.OnInitializedAsync();
        }

        private void AddAttribute() => _model.CategoryAttributeList.Add(new());
        private void RemevoAttribute(CategoryAttributeCreateViewModel vm) 
            => _model.CategoryAttributeList.Remove(vm);
    }
}
