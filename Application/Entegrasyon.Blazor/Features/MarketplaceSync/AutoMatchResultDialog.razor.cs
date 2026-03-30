using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class AutoMatchResultDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public BrandAutoMatchResultDto Result { get; set; } = null!;

    private HashSet<BrandAutoMatchSuggestionDto> _selectedSuggestions = [];

    private void Cancel() => MudDialog.Cancel();

    private void ApproveSelected()
    {
        MudDialog.Close(DialogResult.Ok(_selectedSuggestions.ToList()));
    }

    private void ApproveAll()
    {
        MudDialog.Close(DialogResult.Ok(Result.Suggestions));
    }
}
