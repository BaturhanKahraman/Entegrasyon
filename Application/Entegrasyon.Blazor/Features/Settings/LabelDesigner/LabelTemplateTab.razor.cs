using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Label;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Settings.LabelDesigner;

public partial class LabelTemplateTab : ComponentBase
{
    [Inject] private ILabelTemplateService LabelTemplateService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<LabelTemplateDto> _templates = [];
    private bool _loading = true;
    private bool _designerOpen;
    private LabelTemplateDto? _editingTemplate;

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplates();
    }

    private async Task LoadTemplates()
    {
        _loading = true;
        var result = await LabelTemplateService.GetAllAsync();
        if (result.Success)
            _templates = result.Data ?? [];
        _loading = false;
    }

    private void CreateNew()
    {
        _editingTemplate = null;
        _designerOpen = true;
    }

    private void EditTemplate(LabelTemplateDto template)
    {
        _editingTemplate = template;
        _designerOpen = true;
    }

    private void CloseDesigner()
    {
        _designerOpen = false;
        _editingTemplate = null;
    }

    private async Task OnTemplateSaved()
    {
        _designerOpen = false;
        _editingTemplate = null;
        await LoadTemplates();
    }

    private async Task SetDefault(Guid id)
    {
        var result = await LabelTemplateService.SetAsDefaultAsync(id);
        if (result.Success)
        {
            Snackbar.Add("Varsayılan şablon güncellendi", Severity.Success);
            await LoadTemplates();
        }
        else
        {
            Snackbar.Add(result.Message ?? "İşlem başarısız", Severity.Error);
        }
    }

    private async Task DeleteTemplate(Guid id)
    {
        var result = await LabelTemplateService.DeleteAsync(id);
        if (result.Success)
        {
            Snackbar.Add("Şablon silindi", Severity.Success);
            await LoadTemplates();
        }
        else
        {
            Snackbar.Add(result.Message ?? "Silme başarısız", Severity.Error);
        }
    }
}
