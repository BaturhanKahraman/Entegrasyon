using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Invoicing;

public partial class BulkInvoiceDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private void Cancel() => MudDialog.Cancel();
}
