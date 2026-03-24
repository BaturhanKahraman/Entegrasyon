using System.Globalization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.POS;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.POS;

public partial class POSSummaryDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IPOSSessionManager POSSessionManager { get; set; } = null!;

    [Parameter] public long SessionId { get; set; }

    private static readonly CultureInfo _trCulture = new("tr-TR");
    private POSSummaryDto? _summary;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        var result = await POSSessionManager.GetSessionSummaryAsync(SessionId);
        if (result.Success)
            _summary = result.Data;
        _loading = false;
    }

    private void Close() => MudDialog.Close();
}
