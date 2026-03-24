using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Templates;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MatchedEntityImport;

public partial class MatchedEntityDetailDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public int PackageId { get; set; }

    [Inject] private IMatchedEntityImportManager ImportManager { get; set; } = null!;

    private MatchedEntityPackageDetailDto? _detail;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        var result = await ImportManager.GetPackageDetailAsync(PackageId);
        if (result.Success)
            _detail = result.Data;

        _loading = false;
    }

    private void Cancel() => MudDialog.Cancel();
}
