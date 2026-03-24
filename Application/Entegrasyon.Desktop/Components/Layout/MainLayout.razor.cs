using MudBlazor;

namespace Entegrasyon.Desktop.Components.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = true;
    private bool _isDarkMode;

    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1976d2",
            Secondary = "#ff9800",
            AppbarBackground = "#1976d2"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#90caf9",
            Secondary = "#ffb74d",
            AppbarBackground = "#1e1e2e"
        }
    };

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void ToggleDarkMode()
    {
        _isDarkMode = !_isDarkMode;
    }
}
