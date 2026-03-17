using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Reports;

public partial class SalesReport : ComponentBase
{
    [Inject] private IReportManager ReportManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private DateTime? _startDate = DateTime.Today.AddDays(-30);
    private DateTime? _endDate = DateTime.Today;
    private bool _loading;
    private SalesReportDto? _report;

    private List<ChartSeries> _chartSeries = [];
    private string[] _xAxisLabels = [];
    private readonly ChartOptions _chartOptions = new() { YAxisTicks = 1000, MaxNumYAxisTicks = 10 };

    private async Task GenerateReportAsync()
    {
        if (_startDate is null || _endDate is null)
        {
            Snackbar.Add("Lütfen tarih aralığı seçin.", Severity.Warning);
            return;
        }

        _loading = true;
        try
        {
            var filter = new SalesReportFilterDto(
                DateOnly.FromDateTime(_startDate.Value),
                DateOnly.FromDateTime(_endDate.Value));

            _report = await ReportManager.GetSalesReportAsync(filter);
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

        _xAxisLabels = _report.DailySales
            .Select(d => d.Date.ToString("dd.MM"))
            .ToArray();

        _chartSeries =
        [
            new ChartSeries
            {
                Name = "Ciro (₺)",
                Data = _report.DailySales.Select(d => (double)d.Revenue).ToArray()
            }
        ];
    }
}
