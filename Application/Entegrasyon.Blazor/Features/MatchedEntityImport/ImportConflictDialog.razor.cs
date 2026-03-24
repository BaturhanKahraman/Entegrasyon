using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Templates;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MatchedEntityImport;

public partial class ImportConflictDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public int PackageId { get; set; }
    [Parameter] public string PackageName { get; set; } = null!;
    [Parameter] public List<ImportConflictDto> Conflicts { get; set; } = [];

    [Inject] private IMatchedEntityImportManager ImportManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private readonly Dictionary<int, ConflictResolutionStrategy> _resolutions = new();
    private bool _importing;

    protected override void OnInitialized()
    {
        foreach (var conflict in Conflicts)
        {
            _resolutions[conflict.TemplateEntityId] = ConflictResolutionStrategy.UseExisting;
        }
    }

    private async Task ConfirmImport()
    {
        _importing = true;

        var request = new ImportPackageRequest
        {
            PackageId = PackageId,
            ConflictResolutions = Conflicts.Select(c => new ConflictResolution
            {
                TemplateEntityId = c.TemplateEntityId,
                ExistingEntityId = c.ExistingEntityId,
                Strategy = _resolutions[c.TemplateEntityId]
            }).ToList()
        };

        var result = await ImportManager.ImportPackageAsync(request);

        _importing = false;

        if (result.Success)
        {
            Snackbar.Add($"'{PackageName}' başarıyla import edildi.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add(result.Message, Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
