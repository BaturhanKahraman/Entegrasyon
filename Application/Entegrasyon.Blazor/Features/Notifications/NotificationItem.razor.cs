using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Notifications;

public partial class NotificationItem : ComponentBase
{
    [Parameter, EditorRequired] public Notification Notification { get; set; } = null!;
    [Parameter] public EventCallback<long> OnMarkAsRead { get; set; }

    internal string SeverityIcon => Notification.Severity switch
    {
        NotificationSeverity.Success => Icons.Material.Filled.CheckCircle,
        NotificationSeverity.Warning => Icons.Material.Filled.Warning,
        NotificationSeverity.Error   => Icons.Material.Filled.Error,
        _                            => Icons.Material.Filled.Info
    };

    internal Color SeverityColor => Notification.Severity switch
    {
        NotificationSeverity.Success => Color.Success,
        NotificationSeverity.Warning => Color.Warning,
        NotificationSeverity.Error   => Color.Error,
        _                            => Color.Info
    };

    internal string CategoryLabel => Notification.Category switch
    {
        NotificationCategory.Pazaryeri => "Pazaryeri",
        NotificationCategory.Siparis   => "Sipariş",
        NotificationCategory.Stok      => "Stok",
        _                              => "Sistem"
    };
}
