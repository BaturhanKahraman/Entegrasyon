using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Dashboard;

public partial class DashboardWeeklyChart
{
    [Inject] private IDashboardManager DashboardManager { get; set; } = null!;
    [Inject] private ITenantContext TenantContext { get; set; } = null!;

    private List<ChartSeries> _chartData = [];
    private string[] _xAxisLabels = [];
    private readonly ChartOptions _chartOptions = new() { YAxisTicks = 1000, MaxNumYAxisTicks = 10 };
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        if (!TenantContext.IsInitialized) return;
        var weeklySales = await DashboardManager.GetWeeklySalesAsync();

        _xAxisLabels = weeklySales
            .Select(s => GetTurkishDayAbbr(s.Date.DayOfWeek))
            .ToArray();

        _chartData =
        [
            new ChartSeries
            {
                Name = "Satışlar",
                Data = weeklySales.Select(s => (double)s.Revenue).ToArray()
            }
        ];

        _loading = false;
    }

    private static string GetTurkishDayAbbr(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pzt",
        DayOfWeek.Tuesday => "Sal",
        DayOfWeek.Wednesday => "Çar",
        DayOfWeek.Thursday => "Per",
        DayOfWeek.Friday => "Cum",
        DayOfWeek.Saturday => "Cmt",
        DayOfWeek.Sunday => "Paz",
        _ => ""
    };
}
