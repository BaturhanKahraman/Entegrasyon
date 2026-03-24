using System.Text.Json;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// Persists user settings (server URL, API key, sync interval) to local file system.
/// </summary>
public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        FileSystem.AppDataDirectory, "pos-settings.json");

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

    private void LoadSync()
    {
        try
        {
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
}
