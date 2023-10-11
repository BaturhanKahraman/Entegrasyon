using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.MVC.Components.Razor.CategoryAttribute
{
    public partial class CategoryAttributeAdd
    {
        private CategoryAttributeAddViewModel Model;
        [Parameter]
        public string CatId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Model ??= new();
            await base.OnInitializedAsync();
        }
    }
}
