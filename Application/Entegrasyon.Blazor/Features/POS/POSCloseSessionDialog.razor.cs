using System.Globalization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.POS;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.POS;

public partial class POSCloseSessionDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IPOSSessionManager POSSessionManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public long SessionId { get; set; }

    private static readonly CultureInfo _trCulture = new("tr-TR");
    private POSSummaryDto? _summary;
    private decimal _closingCash;
    private bool _loading = true;
    private bool _processing;

    private decimal CashDifference => _summary != null ? _closingCash - _summary.ExpectedCash : 0;

    protected override async Task OnInitializedAsync()
    {
        var result = await POSSessionManager.GetSessionSummaryAsync(SessionId);
        if (result.Success)
        {
            _summary = result.Data;
            _closingCash = _summary.ExpectedCash;
        }
        _loading = false;
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task Confirm()
    {
        _processing = true;
        try
        {
            var dto = new CloseSessionDto(SessionId, _closingCash);
            var result = await POSSessionManager.CloseSessionAsync(dto);
            if (result.Success)
            {
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message ?? "Kasa kapatilamadi.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _processing = false;
        }
    }
}
