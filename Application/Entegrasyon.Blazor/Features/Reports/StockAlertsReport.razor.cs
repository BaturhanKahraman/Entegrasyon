using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Reports;

public partial class StockAlertsReport : ComponentBase
{
    [Inject] private IReportManager ReportManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private int _threshold = 10;
    private bool _loading;
    private List<StockAlertDto> _alerts = [];
    private int _criticalCount;
    private int _lowCount;

    private async Task LoadAlertsAsync()
    {
        _loading = true;
        try
        {
            _alerts = await ReportManager.GetStockAlertsAsync(_threshold);
            _criticalCount = _alerts.Count(a => a.CurrentStock <= 3);
            _lowCount = _alerts.Count(a => a.CurrentStock > 3);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Uyarilar yuklenemedi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private string RowStyleFunc(StockAlertDto item, int _)
    {
        return item.CurrentStock switch
        {
            <= 0 => "background-color: rgba(244, 67, 54, 0.15);",
            <= 3 => "background-color: rgba(244, 67, 54, 0.08);",
            <= 5 => "background-color: rgba(255, 152, 0, 0.10);",
            _ => string.Empty
        };
    }
}
