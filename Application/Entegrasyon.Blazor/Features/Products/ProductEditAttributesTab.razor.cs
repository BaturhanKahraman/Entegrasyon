using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditAttributesTab
{
    [Parameter] public int CategoryId { get; set; }
    [Parameter] public List<AttributeKeyValueDto> AttributeKeyValues { get; set; } = [];
    [Parameter] public EventCallback<List<AttributeKeyValueDto>> AttributeKeyValuesChanged { get; set; }

    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;

    private List<CategoryAttributeDto> _categoryAttributes = [];
    private bool _loading;
    private int _loadedCategoryId;

    private readonly Dictionary<int, int?> _valueIds = new();
    private readonly Dictionary<int, string?> _customValues = new();

    protected override async Task OnParametersSetAsync()
    {
        if (CategoryId != _loadedCategoryId)
        {
            _loadedCategoryId = CategoryId;
            await LoadAttributes();
        }
    }

    private async Task LoadAttributes()
    {
        _loading = true;
        _categoryAttributes = [];
        _valueIds.Clear();
        _customValues.Clear();

        if (CategoryId == 0)
        {
            _loading = false;
            return;
        }

        var result = await AttributeManager.GetCategoryAttributesByCategory(CategoryId);
        if (result.Success && result.Data is not null)
        {
            _categoryAttributes = result.Data
                .Where(a => !a.IsVarianter && !a.IsSlicer)
                .ToList();

            foreach (var akv in AttributeKeyValues)
            {
                _valueIds[akv.CategoryAttributeId] = akv.AttributeValueId;
                _customValues[akv.CategoryAttributeId] = akv.CustomValue;
            }
        }

        _loading = false;
        await PushChanges();
    }

    private int? GetValueId(int attrId) => _valueIds.GetValueOrDefault(attrId);
    private string GetCustomValue(int attrId) => _customValues.GetValueOrDefault(attrId) ?? string.Empty;

    private async Task SetValueId(int attrId, int? value)
    {
        _valueIds[attrId] = value;
        await PushChanges();
    }

    private async Task SetCustomValue(int attrId, string? value)
    {
        _customValues[attrId] = value;
        await PushChanges();
    }

    private async Task PushChanges()
    {
        var updated = _categoryAttributes
            .Select(a => new AttributeKeyValueDto(
                CategoryAttributeId: a.Id,
                CategoryAttributeName: a.CategoriyAttributeHumanized,
                AttributeValueId: _valueIds.GetValueOrDefault(a.Id),
                AttributeValueName: string.Empty,
                IsRequired: a.IsRequired,
                CustomValue: _customValues.GetValueOrDefault(a.Id) ?? string.Empty))
            .Where(akv => akv.AttributeValueId.HasValue || !string.IsNullOrWhiteSpace(akv.CustomValue))
            .ToList();

        await AttributeKeyValuesChanged.InvokeAsync(updated);
    }
}
