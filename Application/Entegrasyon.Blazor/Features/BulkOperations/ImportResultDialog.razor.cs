using Entegrasyon.Entity.Dtos.BulkOperations;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BulkOperations;

public partial class ImportResultDialog : ComponentBase
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public BulkImportResultDto ImportResult { get; set; } = null!;

    private void Close() => MudDialog.Close();
}
