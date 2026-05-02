using System.Text.Json;

namespace Entegrasyon.Desktop.Services;

public sealed record DeviceCredentials(
    int DeviceId,
    int TenantId,
    string DeviceApiKey,
    string ServerBaseUrl);

/// <summary>
/// Cihaz kimliklerini OS-spesifik kullanıcı veri klasöründe saklar.
/// İlk sürüm dosya tabanlı (production'da DPAPI/Keychain/libsecret'a geçilmeli).
/// </summary>
public sealed class DeviceCredentialStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Entegrasyon", "credentials.json");

    public DeviceCredentials? TryLoad()
    {
        if (!File.Exists(Path)) return null;
        try
        {
            var json = File.ReadAllText(Path);
            return JsonSerializer.Deserialize<DeviceCredentials>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(DeviceCredentials credentials)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(credentials, JsonOpts));
    }

    public void Clear()
    {
        if (File.Exists(Path)) File.Delete(Path);
    }
}
