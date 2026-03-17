using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Dashboard;

public partial class DashboardRecentActivity
{
    [Inject] private IDashboardManager DashboardManager { get; set; } = null!;

    private List<ActivityViewModel> _activities = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        var data = await DashboardManager.GetRecentActivitiesAsync();
        _activities = data
            .Select(a => new ActivityViewModel(
                Message: a.Content,
                Time: FormatRelativeTime(a.CreatedAt),
                Color: GetLogColor((LogType)a.LogType),
                Icon: GetLogIcon((LogAction)a.LogAction)))
            .ToList();
        _loading = false;
    }

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

    private record ActivityViewModel(string Message, string Time, Color Color, string Icon);
}
