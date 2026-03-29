using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryWizardAttributesStep
{
    [Parameter] public List<AttributeModel> Attributes { get; set; } = [];
    [Parameter] public EventCallback<List<AttributeModel>> AttributesChanged { get; set; }
    [Parameter] public EventCallback OnFieldChanged { get; set; }

    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<AppCategoryAttribute> _allAttributes = [];
    private string? _validationError;
    private int _tempIdCounter = -1;

    protected override async Task OnInitializedAsync()
    {
        var result = await AttributeManager.GetCategoryAttributes();
        if (result.Success && result.Data is not null)
            _allAttributes = result.Data;
    }

    private void AddNewAttribute()
    {
        Attributes.Add(new AttributeModel { IsExisting = false });
        OnFieldChanged.InvokeAsync();
    }

    private void AddExistingAttribute()
    {
        Attributes.Add(new AttributeModel { IsExisting = true });
        OnFieldChanged.InvokeAsync();
    }

    private void RemoveAttribute(int index)
    {
        Attributes.RemoveAt(index);
        OnFieldChanged.InvokeAsync();
    }

    private void OnExistingAttributeSelected(AppCategoryAttribute? attr, AttributeModel model)
    {
        if (attr is null) return;
        model.SelectedAttribute = attr;
        model.Id = attr.Id;
        model.CategoryAttributeKey = attr.CategoryAttributeKey ?? "";
        model.CategoryAttributeHumanized = attr.CategoryAttributeHumanized ?? "";
        model.AllowCustom = attr.AllowCustom;
        model.Values = attr.CategoryAttributeValues
            .Select(v => new ValueModel { Id = v.Id, Value = v.Name ?? string.Empty })
            .ToList();
        OnFieldChanged.InvokeAsync();
    }

    private async Task OnVarianterChanged(bool value, AttributeModel model)
    {
        if (value && value != model.IsVarianter)
        {
            var confirm = await DialogService.ShowMessageBox(
                "Uyarı", "Bu değişiklik mevcut ürünleri etkileyebilir. Devam etmek istiyor musunuz?",
                yesText: "Evet", cancelText: "İptal");
            if (confirm != true) return;
        }

        if (value)
        {
            foreach (var a in Attributes.Where(a => a != model))
                a.IsVarianter = false;
            model.IsSlicer = false;
        }
        model.IsVarianter = value;
        await OnFieldChanged.InvokeAsync();
    }

    private async Task OnSlicerChanged(bool value, AttributeModel model)
    {
        if (value && value != model.IsSlicer)
        {
            var confirm = await DialogService.ShowMessageBox(
                "Uyarı", "Bu değişiklik mevcut ürünleri etkileyebilir. Devam etmek istiyor musunuz?",
                yesText: "Evet", cancelText: "İptal");
            if (confirm != true) return;
        }

        if (value)
        {
            foreach (var a in Attributes.Where(a => a != model))
                a.IsSlicer = false;
            model.IsVarianter = false;
        }
        model.IsSlicer = value;
        await OnFieldChanged.InvokeAsync();
    }

    private Task<IEnumerable<AppCategoryAttribute>> SearchAttributes(string value, CancellationToken ct)
    {
        var usedIds = Attributes
            .Where(a => a.IsExisting && a.Id > 0)
            .Select(a => a.Id)
            .ToHashSet();

        if (string.IsNullOrEmpty(value))
            return Task.FromResult(_allAttributes.Where(a => !usedIds.Contains(a.Id)));

        return Task.FromResult(_allAttributes
            .Where(a => !usedIds.Contains(a.Id) &&
                        (a.CategoryAttributeHumanized ?? "").Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task AddValue(AttributeModel attr)
    {
        if (string.IsNullOrWhiteSpace(attr.NewValueText)) return;

        attr.Values.Add(new ValueModel
        {
            Id = _tempIdCounter--,
            Value = attr.NewValueText.Trim()
        });
        attr.NewValueText = string.Empty;
        await InvokeAsync(StateHasChanged);
        await OnFieldChanged.InvokeAsync();
    }

    private void RemoveValue(AttributeModel attr, ValueModel value)
    {
        attr.Values.Remove(value);
        OnFieldChanged.InvokeAsync();
    }

    public bool Validate()
    {
        _validationError = GetValidationError();
        return _validationError is null;
    }

    public string? GetValidationError()
    {
        foreach (var attr in Attributes)
        {
            if (attr.IsExisting && attr.Id <= 0)
                return "Tüm 'Mevcut Özellik' alanlarında bir özellik seçilmelidir.";

            if (!attr.IsExisting)
            {
                if (string.IsNullOrWhiteSpace(attr.CategoryAttributeKey))
                    return "Yeni özellikler için anahtar (key) alanı boş bırakılamaz.";
                if (string.IsNullOrWhiteSpace(attr.CategoryAttributeHumanized))
                    return "Yeni özellikler için görünen ad alanı boş bırakılamaz.";
            }
        }
        return null;
    }
}

public class AttributeModel
{
    public int Id { get; set; }
    public bool IsExisting { get; set; }
    public AppCategoryAttribute? SelectedAttribute { get; set; }
    public string CategoryAttributeKey { get; set; } = string.Empty;
    public string CategoryAttributeHumanized { get; set; } = string.Empty;
    public bool AllowCustom { get; set; } = true;
    public bool IsRequired { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
    public List<ValueModel> Values { get; set; } = [];
    public string NewValueText { get; set; } = string.Empty;
}

public class ValueModel
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
