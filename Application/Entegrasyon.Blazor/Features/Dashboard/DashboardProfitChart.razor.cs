using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Dashboard;

public partial class DashboardProfitChart : ComponentBase
{
    [Inject] private IReportManager ReportManager { get; set; } = default!;

    private List<ChartSeries> _chartData = [];
    private string[] _xAxisLabels = [];
    private readonly ChartOptions _chartOptions = new() { YAxisTicks = 500, MaxNumYAxisTicks = 10 };
    private bool _loading = true;
    private bool _hasData;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var filter = new SalesReportFilterDto(
                DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                DateOnly.FromDateTime(DateTime.Today));

            var report = await ReportManager.GetSalesReportAsync(filter);

            if (report.DailySales.Any())
            {
                _hasData = true;
                _xAxisLabels = report.DailySales
                    .Select(d => d.Date.ToString("dd.MM"))
                    .ToArray();

                _chartData =
                [
                    new ChartSeries
                    {
                        Name = "Ciro (TL)",
                        Data = report.DailySales.Select(d => (double)d.Revenue).ToArray()
                    }
                ];
            }
        }
        catch
        {
            // Dashboard chart hatasi kullaniciyi engellemez
        }
        finally
        {
            _loading = false;
        }
    }
}
