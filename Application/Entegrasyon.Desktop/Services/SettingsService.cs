using System.Text.Json;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// Persists user settings (server URL, API key, sync interval) to local file system.
/// </summary>
public class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Entegrasyon");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "pos-settings.json");

    private PosSettings _settings = new();
    private bool _loaded;

    public PosSettings Settings
    {
        get
        {
            if (!_loaded) LoadSync();
            return _settings;
        }
    }

    public PosSettings Load()
    {
        if (!_loaded) LoadSync();
        return _settings;
    }

    private void LoadSync()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _settings = JsonSerializer.Deserialize<PosSettings>(json) ?? new PosSettings();
            }
        }
        catch
        {
            _settings = new PosSettings();
        }
        _loaded = true;
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(SettingsPath, json);
    }
}

public class PosSettings
{
    public string ServerUrl { get; set; } = "https://localhost:5001";
    public string ApiKey { get; set; } = string.Empty;
    public int SyncIntervalSeconds { get; set; } = 60;
    public int PrintAgentPort { get; set; } = 19100;
    public string StoreName { get; set; } = "Magaza";
    public string? BranchOfficeId { get; set; }

    /// <summary>
    /// Velopack güncelleme kaynağı URL'si.
    /// GitHub: "https://github.com/owner/repo"
    /// Özel sunucu: "https://updates.example.com/releases"
    /// </summary>
    public string UpdateUrl { get; set; } = string.Empty;

    /// <summary>
    /// Otomatik güncelleme kontrolü aktif mi?
    /// </summary>
    public bool AutoUpdateEnabled { get; set; } = true;
}
