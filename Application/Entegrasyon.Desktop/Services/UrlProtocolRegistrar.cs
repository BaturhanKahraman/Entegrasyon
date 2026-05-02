using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// entegrasyon-print:// custom URL scheme'ini OS'a kaydeder.
/// İlk uygulama çalıştırılışında ya da Velopack install hook'undan çağrılır.
/// Per-user kayıt — admin yetkisi gerekmez.
/// </summary>
public sealed class UrlProtocolRegistrar(ILogger<UrlProtocolRegistrar> logger)
{
    public const string Scheme = "entegrasyon-print";

    public void RegisterIfMissing()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                RegisterWindows();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                RegisterLinux();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                logger.LogInformation("macOS protocol kaydı Info.plist üzerinden bundle-time yapılır.");
            else
                logger.LogWarning("Bilinmeyen platform — URL protocol kaydı atlandı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "URL protocol kaydı başarısız.");
        }
    }

    [SupportedOSPlatform("windows")]
    private void RegisterWindows()
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
        {
            logger.LogWarning("Process exe yolu bulunamadı.");
            return;
        }

        // HKCU\Software\Classes\entegrasyon-print
        using var schemeKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{Scheme}");
        schemeKey.SetValue("", $"URL:Entegrasyon Print Protocol");
        schemeKey.SetValue("URL Protocol", "");

        using var iconKey = schemeKey.CreateSubKey("DefaultIcon");
        iconKey.SetValue("", $"\"{exePath}\",0");

        using var commandKey = schemeKey.CreateSubKey(@"shell\open\command");
        commandKey.SetValue("", $"\"{exePath}\" \"%1\"");

        logger.LogInformation("Windows: {Scheme}:// kaydı tamam — {Exe}", Scheme, exePath);
    }

    private void RegisterLinux()
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath))
        {
            logger.LogWarning("Process exe yolu bulunamadı.");
            return;
        }

        var appsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "applications");
        Directory.CreateDirectory(appsDir);

        var desktopFilePath = Path.Combine(appsDir, "entegrasyon-desktop.desktop");
        var content = $"""
            [Desktop Entry]
            Name=Entegrasyon Desktop
            Exec="{exePath}" %u
            Type=Application
            Terminal=false
            MimeType=x-scheme-handler/{Scheme};
            Categories=Office;
            """;
        File.WriteAllText(desktopFilePath, content);

        try
        {
            var psi = new ProcessStartInfo("xdg-mime", $"default entegrasyon-desktop.desktop x-scheme-handler/{Scheme}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "xdg-mime default kaydı başarısız (manuel kayıt gerekebilir).");
        }

        logger.LogInformation("Linux: {Scheme}:// kaydı tamam — {DesktopFile}", Scheme, desktopFilePath);
    }
}

