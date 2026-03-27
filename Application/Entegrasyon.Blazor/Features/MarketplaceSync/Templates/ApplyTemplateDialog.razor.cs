using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.Templates;

public partial class ApplyTemplateDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public int MarketPlaceId { get; set; }

    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<CategoryMatchTemplateDto> _templates = [];
    private CategoryMatchTemplateDto? _selectedTemplate;
    private bool _isLoading = true;
    private bool _isApplying;

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplates();
    }

    private async Task LoadTemplates()
    {
        _isLoading = true;
        var result = await CategoryMatchService.GetTemplatesAsync(MarketPlaceId);
        if (result.Success)
            _templates = result.Data;
        _isLoading = false;
    }

    private async Task Apply()
    {
        if (_selectedTemplate is null) return;

        _isApplying = true;

        var result = await CategoryMatchService.ApplyTemplateAsync(_selectedTemplate.Id);
        if (result.Success)
        {
            Snackbar.Add(
                $"Template uygulandi: {result.Data.SuccessCount} basarili, {result.Data.SkippedCount} atlandi, {result.Data.FailedCount} hata",
                Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }

        _isApplying = false;
    }

    private async Task DeleteTemplate(CategoryMatchTemplateDto template)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Template Sil",
            $"'{template.Name}' template'ini silmek istediginize emin misiniz?",
            yesText: "Evet", cancelText: "Iptal");

        if (confirmed is true)
        {
            var result = await CategoryMatchService.DeleteTemplateAsync(template.Id);
            if (result.Success)
            {
                Snackbar.Add("Template silindi.", Severity.Success);
                await LoadTemplates();
            }
            else
            {
                Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            }
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
