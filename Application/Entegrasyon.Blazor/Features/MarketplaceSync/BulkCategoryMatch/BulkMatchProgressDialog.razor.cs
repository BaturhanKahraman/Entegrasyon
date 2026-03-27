using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.BulkCategoryMatch;

public partial class BulkMatchProgressDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public BulkCategoryMatchResultDto Result { get; set; } = new();

    private void Close() => MudDialog.Close(DialogResult.Ok(true));
}
