using AutoMapper;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.MVC.ViewModels.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Shared.Helpers;
using Shared.Results;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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

        private const int RandomFormUniqueIdLength = 6;
        private bool IsUpdating = false;

        private ValidationMessageStore _messageStore;
        private EditContext _editContext;
        private readonly Queue<string> _addedItemIds = new();
        private SelectExistingAttributeModal _selectExistingAttributeModal;
        protected override void OnInitialized()
        {
            //gelen değerler doluysa random formuniqueid atanıp tagify yapılacak.
            Model.CategoryId ??= CatId;
            IsUpdating = Model.CategoryAttributeList.Any();
            if (IsUpdating)
            {
                Model.CategoryAttributeList.ForEach(ca => {
                    ca.FormUniqueId = GetFormUniqueId();
                    ca.IsAddedAfterward = false;
                });
            }
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
            else
                Model.CategoryAttributeList.ForEach(async ca=>
                    await Tagify(ca.FormUniqueId,
                    JsonSerializer.Serialize(ca.CategoryAttributeValues.Select(
                        cav=>new TagifyValue(cav.Id.Value,cav.Name)))));
        }
        private async Task OnValidSubmit()
        {
            Model.CategoryAttributeList.ForEach(ca =>
            {
                if (!ca.AllowCustom && !string.IsNullOrEmpty(ca.CustomValues))
                {
                    ca.CategoryAttributeValues = JsonSerializer
                            .Deserialize<List<TagifyValue>>(ca.CustomValues)
                            .Select(x=>new CategoryAttributeValueViewModel(x.id,x.value)).ToList();
                }
            });
            var dto = _mapper.Map<List<AddCategoryAttributeDto>>(Model.CategoryAttributeList);
            Shared.Results.IResult result;
            try
            {
                result = await _cacManager.AddCategoryAttributeForCategory(Model.CategoryId.Value, dto);
                if (result.Success)
                {
                    //success
                    _navigationManager.NavigateTo("/CategoryAttributes/SuccesfullyAdded",true);
                }
            }
            catch (Exception e)
            {
                //ignore
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
            { FormUniqueId = GetFormUniqueId(), IsAddedAfterward = true };
            Model.CategoryAttributeList.Add(vm);
            _addedItemIds.Enqueue(vm.FormUniqueId);
        }

        private string GetFormUniqueId()
            =>_randomGenerator.GetRandomCode(RandomFormUniqueIdLength, includeNumbers: false);
        

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
        private async Task Tagify(string formId,string initialJsonValue)
        {
            var inputId = $"#{formId}_CustomValues";
            await _js.InvokeVoidAsync("initializeTagify", inputId,initialJsonValue);
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


