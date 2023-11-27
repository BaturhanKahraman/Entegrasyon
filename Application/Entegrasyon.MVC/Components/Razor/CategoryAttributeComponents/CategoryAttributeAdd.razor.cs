using AutoMapper;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Shared.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.MVC.Components.Razor.CategoryAttributeComponents
{
    public partial class CategoryAttributeAdd:IDisposable
    {
        [Parameter]
        public int CatId { get; set; }
        [Parameter]
        public string CategoryName { get; set; }
        [Inject]
        private IRandomGenerator _randomGenerator { get; set; }
        [Inject]
        private NavigationManager _navigationManager { get; set; }
        [Inject]
        private IJSRuntime _js { get; set; }
        [Inject]
        private IMapper _mapper { get; set; }
        [Inject]
        private CategoryAttributeCategoryManager _cacManager { get; set; }
        [Parameter]
        public CategoryAttributeAddViewModel Model { get; set; } = new();

        private ValidationMessageStore _messageStore;
        private EditContext _editContext;
        private readonly Queue<string> _addedItemIds = new();
        private SelectExistingAttributeModal _selectExistingAttributeModal;
        protected override void OnInitialized()
        {
            Model.CategoryId ??= CatId;
            _editContext = new(Model);
            _messageStore = new(_editContext);
            _editContext.OnValidationRequested += _editContext_OnValidationRequested;
        }

        private void _editContext_OnValidationRequested(object sender, ValidationRequestedEventArgs e)
        {
            _messageStore.Clear();
            bool isModelValid = ValidateCompleteModel();
            if (!isModelValid)
                StateHasChanged();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                while (_addedItemIds.Any())
                {
                    string formId = _addedItemIds.Dequeue();
                    await Tagify(formId);
                }
            }
        }
        private async Task OnValidSubmit()
        {
            Model.CategoryAttributeList.ForEach(ca =>
            {
                if (!ca.AllowCustom && !string.IsNullOrEmpty(ca.CustomValues))
                {
                    //json value atıyor burada parçalanıp tek tek eklenecek.
                    ca.CategoryAttributeValues = JsonConvert
                            .DeserializeObject<List<TagifyValue>>(ca.CustomValues)
                            .Select(x=>new CategoryAttributeValueViewModel(null,x.Value)).ToList();
                }
            });
            var dto = _mapper.Map<List<AddCategoryAttributeDto>>(Model.CategoryAttributeList);
            var result = await _cacManager.AddCategoryAttributeForCategory(Model.CategoryId.Value, dto);
            if (result.Success)
            {
                //success
                _navigationManager.NavigateTo("/CategoryAttributes/SuccesffullyAdded");
            }
            else
            {
                _messageStore.Add(null, result.Message);
            }
        }

        private bool ValidateCompleteModel()
        {
            ValidationContext vc = new(Model);
            List<ValidationResult> results = new();
            var result = Validator.TryValidateObject(Model, vc, results, true);

            bool anyItemValid = true;
            foreach (var item in Model.CategoryAttributeList)
            {
                ValidationContext itemContext = new ValidationContext(item);
                bool isItemValid = Validator.TryValidateObject(item, itemContext, results, true);
                if (!isItemValid)
                {
                    anyItemValid = false;
                    foreach (var validationResult in results)
                    {
                        foreach (var memberName in validationResult.MemberNames)
                        {
                            var fieldIdentifier = new FieldIdentifier(item, memberName);
                            _messageStore.Add(fieldIdentifier, validationResult.ErrorMessage);
                        }
                    }
                }
            }
            return result && anyItemValid;
        }

        private void AddAttribute()
        {
            var vm = new CategoryAttributeCreateViewModel
            { FormUniqueId = _randomGenerator.GetRandomCode(6, includeNumbers: false) };
            Model.CategoryAttributeList.Add(vm);
            _addedItemIds.Enqueue(vm.FormUniqueId);
        }
        private async Task ChangeAllowCustom(CategoryAttributeCreateViewModel item, bool status)
        {
            await ToggleDisabilityTagify(item.FormUniqueId, status);
            item.AllowCustom = status;
        }
        private async Task Tagify(string formId)
        {
            var inputId = $"#{formId}_CustomValues";
            await _js.InvokeVoidAsync("initializeTagify", inputId);
        }
        private async Task ToggleDisabilityTagify(string formId, bool status)
        {
            string inputId = $"#{formId}_CustomValues";
            await _js.InvokeVoidAsync("setDisable", inputId, status);
        }
        private void RemevoAttribute(CategoryAttributeCreateViewModel vm)
            => Model.CategoryAttributeList.Remove(vm);
        private async Task OpenExistedCatAttrSelectModal()
        {
            await _selectExistingAttributeModal.Open();
        }
        public void Dispose()
        {
            _editContext.OnValidationRequested -= _editContext_OnValidationRequested;
        }
    }
}


public sealed record TagifyValue(string Value);