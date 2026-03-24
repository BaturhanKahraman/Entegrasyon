using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Settings;

public partial class DesktopAppSettings : ComponentBase
{
    /// <summary>
    /// Current application version displayed on the page.
    /// </summary>
    private const string AppVersion = "1.0.0-preview";

    /// <summary>
    /// Default Print Agent port used by the embedded agent in the desktop app.
    /// </summary>
    private const int PrintAgentPort = 19100;

    /// <summary>
    /// Mock download URL for Windows installer.
    /// Will be replaced with a real URL when release infrastructure is ready.
    /// </summary>
    private const string WindowsDownloadUrl = "#download-windows";

    /// <summary>
    /// Mock download URL for Linux package.
    /// Will be replaced with a real URL when release infrastructure is ready.
    /// </summary>
    private const string LinuxDownloadUrl = "#download-linux";

    /// <summary>
    /// Whether download links point to real files.
    /// Returns false when URLs are still mock placeholders.
    /// </summary>
    private static bool IsDownloadAvailable =>
        !WindowsDownloadUrl.StartsWith('#') && !LinuxDownloadUrl.StartsWith('#');

    // MudBlazor does not have built-in Windows/Linux icons; use Material Design equivalents
    private const string WindowsIcon = Icons.Material.Filled.DesktopWindows;
    private const string LinuxIcon = Icons.Material.Filled.Computer;
}
