using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class UpgradePromptDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }

    [Parameter] public string FeatureName { get; set; } = string.Empty;

    private void Close()
    {
        MudDialog?.Close(DialogResult.Cancel());
    }
}
