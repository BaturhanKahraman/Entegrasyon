using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NetTopologySuite.Index.HPRtree;
using Shared.Helpers;

namespace Entegrasyon.MVC.Components.Razor.CategoryAttribute
{
    public partial class CategoryAttributeAdd
    {
        [Parameter]
        public string CatId { get; set; }
        [Parameter]
        public string CategoryName { get; set; }
        [Inject]
        private IRandomGenerator _randomGenerator { get; set; }
        [Inject]
        private IJSRuntime _js { get; set; }
        private CategoryAttributeAddViewModel _model;
        private Queue<string> _addedItemIds = new();
        protected override async Task OnInitializedAsync()
        {
            _model ??= new();
            await base.OnInitializedAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if(!firstRender)
            {
                while(_addedItemIds.Any())
                {
                    string formId = _addedItemIds.Dequeue();
                    await Tagify(formId);
                }
            }
        }
        private void AddAttribute()
        {
            var vm = new CategoryAttributeCreateViewModel 
            { FormUniqueId = _randomGenerator.GetRandomCode(6,includeNumbers:false) };
            _model.CategoryAttributeList.Add(vm);
            _addedItemIds.Enqueue(vm.FormUniqueId);
        }
        private async Task ChangeAllowCustom(CategoryAttributeCreateViewModel item,bool status)
        {
            await ToggleDisabilityTagify(item.FormUniqueId,status);
            item.AllowCustom = status;
        }
        private async Task Tagify(string formId)
        {
            var inputId = $"#{formId}_CustomValues";
            await _js.InvokeVoidAsync("initializeTagify",inputId);
        }
        private async Task ToggleDisabilityTagify(string formId,bool status)
        {
            string inputId = $"#{formId}_CustomValues";
            await _js.InvokeVoidAsync("setDisable",inputId,status);
        }
        private void RemevoAttribute(CategoryAttributeCreateViewModel vm)
            => _model.CategoryAttributeList.Remove(vm);
    }
}
