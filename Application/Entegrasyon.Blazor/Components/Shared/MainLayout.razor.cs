using Entegrasyon.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class MainLayout
{
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    private bool _drawerOpen = true;
    private bool _notificationPanelOpen = false;
    private bool _isDarkMode = false;
    private int _notificationCount = 3;
    private MudTheme _theme = new();
    private List<NotificationItem> _notifications = [];

    protected override void OnInitialized()
    {
        _theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#1976D2",
                Secondary = "#424242",
                Success = "#4CAF50",
                Info = "#2196F3",
                Warning = "#FF9800",
                Error = "#F44336",
                AppbarBackground = "#1976D2",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#90CAF9",
                Secondary = "#BDBDBD",
                Success = "#81C784",
                Info = "#64B5F6",
                Warning = "#FFB74D",
                Error = "#E57373",
                AppbarBackground = "#212121",
            }
        };

        _notifications =
        [
            new("Yeni sipariş alındı: #TR-12345", "5 dk önce", NotificationType.Info),
            new("Stok uyarısı: 5 ürün kritik seviyede", "1 saat önce", NotificationType.Warning),
            new("Trendyol senkronizasyonu tamamlandı", "2 saat önce", NotificationType.Success)
        ];
    }

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;
    private void ToggleNotificationPanel() => _notificationPanelOpen = !_notificationPanelOpen;
    private void ToggleTheme() => _isDarkMode = !_isDarkMode;

    private void ClearNotifications()
    {
        _notifications.Clear();
        _notificationCount = 0;
        _notificationPanelOpen = false;
    }

    private static string GetNotificationIcon(NotificationType type) => type switch
    {
        NotificationType.Success => Icons.Material.Filled.CheckCircle,
        NotificationType.Warning => Icons.Material.Filled.Warning,
        NotificationType.Error => Icons.Material.Filled.Error,
        _ => Icons.Material.Filled.Info
    };

    private static Color GetNotificationColor(NotificationType type) => type switch
    {
        NotificationType.Success => Color.Success,
        NotificationType.Warning => Color.Warning,
        NotificationType.Error => Color.Error,
        _ => Color.Info
    };

    private async Task HandleLogout()
    {
        if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
        {
            await customAuthStateProvider.UpdateAuthenticationState(null);
        }

        NavigationManager.NavigateTo("/auth/login", forceLoad: true);
    }

    private record NotificationItem(string Message, string Time, NotificationType Type);

    private enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
