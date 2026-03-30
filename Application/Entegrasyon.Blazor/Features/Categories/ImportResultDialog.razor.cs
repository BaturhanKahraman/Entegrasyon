using Entegrasyon.Entity.Dtos.MasterCatalog;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class ImportResultDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public ImportResultDto Result { get; set; } = null!;

    private void Close() => MudDialog.Close();
}
