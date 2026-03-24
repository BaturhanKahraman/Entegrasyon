using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Reports;

public partial class ProfitLossReport : ComponentBase
{
    [Inject] private IReportManager ReportManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private DateTime? _startDate = DateTime.Today.AddDays(-30);
    private DateTime? _endDate = DateTime.Today;
    private int? _selectedMarketPlaceId;
    private bool _loading;
    private ProfitLossReportDto? _report;

    private List<ChartSeries> _chartSeries = [];
    private string[] _xAxisLabels = [];
    private readonly ChartOptions _chartOptions = new() { YAxisTicks = 1000, MaxNumYAxisTicks = 10 };

    private async Task GenerateReportAsync()
    {
        if (_startDate is null || _endDate is null)
        {
            Snackbar.Add("Lutfen tarih araligi secin.", Severity.Warning);
            return;
        }

        _loading = true;
        try
        {
            var filter = new ProfitLossReportFilterDto(
                DateOnly.FromDateTime(_startDate.Value),
                DateOnly.FromDateTime(_endDate.Value),
                _selectedMarketPlaceId);

            _report = await ReportManager.GetProfitLossReportAsync(filter);
            BuildChart();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Rapor olusturulamadi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void BuildChart()
    {
        if (_report is null || !_report.ByMarketplace.Any()) return;

        _xAxisLabels = _report.ByMarketplace
            .Select(x => x.MarketPlaceName)
            .ToArray();

        _chartSeries =
        [
            new ChartSeries
            {
                Name = "Ciro (TL)",
                Data = _report.ByMarketplace.Select(x => (double)x.Revenue).ToArray()
            },
            new ChartSeries
            {
                Name = "Komisyon (TL)",
                Data = _report.ByMarketplace.Select(x => (double)x.Commission).ToArray()
            },
            new ChartSeries
            {
                Name = "Net Kar (TL)",
                Data = _report.ByMarketplace.Select(x => (double)x.NetProfit).ToArray()
            }
        ];
    }
}
