using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Dashboard;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Dashboard;

public partial class Index : IAsyncDisposable
{
    [Inject] private IDashboardManager DashboardManager { get; set; } = null!;
    [Inject] private IProductSyncManager ProductSyncManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool loading = true;
    private DashboardStatsDto stats = new(0, 0, 0, 0m, 0, 0);
    private List<ChartSeries> salesChartData = [];
    private string[] xAxisLabels = [];
    private readonly ChartOptions chartOptions = new() { YAxisTicks = 1000, MaxNumYAxisTicks = 10 };
    private List<MarketplaceStatusViewModel> marketplaceStatuses = [];
    private List<ActivityViewModel> recentActivities = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        loading = true;

        var dashboard = await DashboardManager.GetDashboardAsync();

        stats = dashboard.Stats;

        // Haftalık satış grafiği
        xAxisLabels = dashboard.WeeklySales
            .Select(s => GetTurkishDayAbbr(s.Date.DayOfWeek))
            .ToArray();

        salesChartData =
        [
            new ChartSeries
            {
                Name = "Satışlar",
                Data = dashboard.WeeklySales.Select(s => (double)s.Revenue).ToArray()
            }
        ];

        // Marketplace durumları
        marketplaceStatuses = dashboard.MarketplaceStatuses
            .Select(m => new MarketplaceStatusViewModel(
                Name: m.Name,
                Icon: GetMarketplaceIcon(m.Name),
                Color: GetMarketplaceColor(m.Name),
                SyncedCount: m.SyncedCount,
                PendingCount: m.PendingCount,
                FailedCount: m.FailedCount,
                TotalProducts: m.TotalProducts,
                MarketPlaceId: m.MarketPlaceId))
            .ToList();

        // Son aktiviteler
        recentActivities = dashboard.RecentActivities
            .Select(a => new ActivityViewModel(
                Message: a.Content,
                Time: FormatRelativeTime(a.CreatedAt),
                Color: GetLogColor((LogType)a.LogType),
                Icon: GetLogIcon((LogAction)a.LogAction)))
            .ToList();

        loading = false;
    }

    private async Task SyncMarketplaces()
    {
        Snackbar.Add("Pazaryeri senkronizasyonu başlatıldı", Severity.Info);
        var result = await ProductSyncManager.SyncAllPendingAsync(1);
        if (result.Success)
        {
            Snackbar.Add("Senkronizasyon tamamlandı", Severity.Success);
            await LoadDashboardData();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add($"Senkronizasyon hatası: {result.Message}", Severity.Error);
        }
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

    private static string GetMarketplaceIcon(string name) => name.ToLowerInvariant() switch
    {
        "trendyol" => Icons.Material.Filled.Store,
        "hepsiburada" => Icons.Material.Filled.ShoppingCart,
        "n11" => Icons.Material.Filled.Storefront,
        "amazon" => Icons.Material.Filled.LocalMall,
        _ => Icons.Material.Filled.Store
    };

    private static Color GetMarketplaceColor(string name) => name.ToLowerInvariant() switch
    {
        "trendyol" => Color.Primary,
        "hepsiburada" => Color.Secondary,
        "n11" => Color.Info,
        "amazon" => Color.Tertiary,
        _ => Color.Default
    };

    private static Color GetLogColor(LogType logType) => logType switch
    {
        LogType.Product => Color.Success,
        LogType.Order => Color.Info,
        LogType.Sale => Color.Primary,
        LogType.StockSync => Color.Warning,
        LogType.Marketplace => Color.Secondary,
        LogType.Error => Color.Error,
        _ => Color.Default
    };

    private static string GetLogIcon(LogAction action) => action switch
    {
        LogAction.Add => Icons.Material.Filled.AddCircle,
        LogAction.Update => Icons.Material.Filled.Edit,
        LogAction.Delete => Icons.Material.Filled.Delete,
        LogAction.Sync => Icons.Material.Filled.Sync,
        LogAction.Import => Icons.Material.Filled.CloudDownload,
        LogAction.Publish => Icons.Material.Filled.CloudUpload,
        _ => Icons.Material.Filled.Info
    };

    private static string FormatRelativeTime(DateTimeOffset createdAt)
    {
        var diff = DateTimeOffset.UtcNow - createdAt;

        return diff.TotalMinutes switch
        {
            < 1 => "az önce",
            < 60 => $"{(int)diff.TotalMinutes} dk önce",
            < 1440 => $"{(int)diff.TotalHours} saat önce",
            _ => $"{(int)diff.TotalDays} gün önce"
        };
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private record MarketplaceStatusViewModel(
        string Name, string Icon, Color Color,
        int SyncedCount, int PendingCount, int FailedCount,
        int TotalProducts, int MarketPlaceId);

    private record ActivityViewModel(string Message, string Time, Color Color, string Icon);
}
