using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.Templates;

public partial class SaveTemplateDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public int MarketPlaceId { get; set; }

    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private string _name = string.Empty;
    private string? _description;
    private bool _isSaving;

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_name)) return;

        _isSaving = true;

        var dto = new SaveCategoryMatchTemplateDto
        {
            Name = _name,
            Description = _description,
            MarketPlaceId = MarketPlaceId
        };

        var result = await CategoryMatchService.SaveTemplateAsync(dto);

        if (result.Success)
        {
            Snackbar.Add(result.Message ?? "Basarili", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }

        _isSaving = false;
    }

    private void Cancel() => MudDialog.Cancel();
}
