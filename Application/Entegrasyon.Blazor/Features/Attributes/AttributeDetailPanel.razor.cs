using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.Attributes;

public partial class AttributeDetailPanel
{
    [Parameter] public AppCategoryAttribute? SelectedAttribute { get; set; }
    [Parameter] public EventCallback OnAttributeChanged { get; set; }

    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private bool _isEditing = false;
    private bool _saving = false;
    private string _editHumanized = string.Empty;
    private string _editKey = string.Empty;
    private bool _editAllowCustom = false;
    private List<CategoryAttributeValue> _editValues = [];
    private string _newValueName = string.Empty;

    private async Task StartEdit()
    {
        if (SelectedAttribute is null) return;
        var result = await AttributeManager.GetCategoryAttributeById(SelectedAttribute.Id);
        if (!result.Success || result.Data is null)
        {
            Snackbar.Add("Özellik detayları yüklenemedi.", Severity.Error);
            return;
        }
        var attr = result.Data;
        _editHumanized = attr.CategoryAttributeHumanized;
        _editKey = attr.CategoryAttributeKey;
        _editAllowCustom = attr.AllowCustom;
        _editValues = attr.CategoryAttributeValues.ToList();
        _newValueName = string.Empty;
        _isEditing = true;
    }

    private void CancelEdit() => _isEditing = false;

    private async Task SaveEdit()
    {
        if (SelectedAttribute is null) return;
        _saving = true;
        try
        {
            var dto = new EditCategoryAttributeDto(
                Id: SelectedAttribute.Id,
                IsRequired: false,
                AllowCustom: _editAllowCustom,
                IsVarianter: false,
                CategoryAttributeKey: _editKey,
                IsSlicer: false,
                CategoryAttributeHumanized: _editHumanized,
                CategoryAttributeValues: _editValues,
                CategoryId: 0
            );
            var result = await AttributeManager.UpdateCategoryAttribute(dto);
            if (result.Success)
            {
                Snackbar.Add(result.Message ?? "Özellik güncellendi.", Severity.Success);
                _isEditing = false;
                await OnAttributeChanged.InvokeAsync();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Güncelleme başarısız.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task DeleteAttribute()
    {
        if (SelectedAttribute is null) return;
        bool? confirmed = await DialogService.ShowMessageBox(
            "Özelliği Sil",
            $"'{SelectedAttribute.CategoryAttributeHumanized}' özelliğini silmek istiyor musunuz?",
            yesText: "Sil",
            cancelText: "İptal");
        if (confirmed != true) return;

        var result = await AttributeManager.DeleteCategoryAttribute(SelectedAttribute.Id);
        if (result.Success)
        {
            Snackbar.Add(result.Message ?? "Özellik silindi.", Severity.Success);
            await OnAttributeChanged.InvokeAsync();
        }
        else
        {
            Snackbar.Add(result.Message ?? "Silme başarısız.", Severity.Error);
        }
    }

    private void AddValue()
    {
        if (string.IsNullOrWhiteSpace(_newValueName)) return;
        _editValues.Add(new CategoryAttributeValue { Name = _newValueName.Trim() });
        _newValueName = string.Empty;
    }

    private void RemoveValue(int index) => _editValues.RemoveAt(index);
}
