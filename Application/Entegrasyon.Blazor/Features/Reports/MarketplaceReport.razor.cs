using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Reports;

public partial class MarketplaceReport : ComponentBase
{
    [Inject] private IReportManager ReportManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private bool _loading;
    private MarketplaceReportDto? _report;
    private double[] _chartData = [];
    private string[] _chartLabels = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        _loading = true;
        try
        {
            _report = await ReportManager.GetMarketplaceReportAsync();
            BuildChart();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Rapor oluşturulamadı: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void BuildChart()
    {
        if (_report is null) return;

        _chartData =
        [
            _report.Summary.PublishedCount,
            _report.Summary.PendingCount,
            _report.Summary.FailedCount,
            _report.Summary.RejectedCount
        ];

        _chartLabels = ["Yayında", "Bekliyor", "Başarısız", "Reddedilen"];
    }
}
