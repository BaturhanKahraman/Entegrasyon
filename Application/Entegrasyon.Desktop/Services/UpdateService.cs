using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// Velopack ile otomatik güncelleme servisi.
/// Uygulama açılışında ve periyodik olarak güncelleme kontrol eder.
/// Delta update desteği — sadece değişen kısım indirilir.
/// </summary>
public class UpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly SettingsService _settingsService;
    private UpdateManager? _updateManager;
    private UpdateInfo? _pendingUpdate;

    /// <summary>
    /// Bekleyen güncelleme var mı?
    /// </summary>
    public bool HasPendingUpdate => _pendingUpdate != null;

    /// <summary>
    /// Bekleyen güncelleme versiyon bilgisi.
    /// </summary>
    public string? PendingVersion => _pendingUpdate?.TargetFullRelease?.Version?.ToString();

    /// <summary>
    /// Güncelleme durumu değiştiğinde tetiklenir.
    /// </summary>
    public event Action? OnUpdateAvailable;

    /// <summary>
    /// Güncelleme indirme ilerlemesi (0-100).
    /// </summary>
    public int DownloadProgress { get; private set; }

    public UpdateService(ILogger<UpdateService> logger, SettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Güncelleme kaynağını yapılandır ve kontrol et.
    /// Kaynak: GitHub Releases veya özel HTTP sunucusu.
    /// </summary>
    public async Task CheckForUpdatesAsync()
    {
        try
        {
            var settings = _settingsService.Load();
            var updateUrl = settings.UpdateUrl;

            if (string.IsNullOrWhiteSpace(updateUrl))
            {
                _logger.LogInformation("Güncelleme URL'si yapılandırılmamış, kontrol atlanıyor.");
                return;
            }

            IUpdateSource source;

            if (updateUrl.Contains("github.com"))
            {
                // GitHub Releases: "https://github.com/owner/repo"
                source = new GithubSource(updateUrl, null, false);
            }
            else
            {
                // Özel HTTP sunucusu: "https://updates.example.com/releases"
                source = new SimpleWebSource(updateUrl);
            }

            _updateManager = new UpdateManager(source);

            // Velopack tarafından yönetilen bir kurulumda mıyız?
            if (!_updateManager.IsInstalled)
            {
                _logger.LogInformation("Uygulama Velopack ile kurulmamış, güncelleme kontrolü atlanıyor.");
                return;
            }

            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();

            if (_pendingUpdate != null)
            {
                _logger.LogInformation("Güncelleme bulundu: v{Version}", PendingVersion);
                OnUpdateAvailable?.Invoke();
            }
            else
            {
                _logger.LogInformation("Uygulama güncel.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Güncelleme kontrolü sırasında hata oluştu.");
        }
    }

    /// <summary>
    /// Güncellemeyi indir.
    /// </summary>
    public async Task DownloadUpdateAsync()
    {
        if (_updateManager == null || _pendingUpdate == null) return;

        try
        {
            await _updateManager.DownloadUpdatesAsync(
                _pendingUpdate,
                progress => DownloadProgress = progress);

            _logger.LogInformation("Güncelleme indirildi: v{Version}", PendingVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Güncelleme indirme hatası.");
        }
    }

    /// <summary>
    /// Güncellemeyi uygula ve uygulamayı yeniden başlat.
    /// </summary>
    public void ApplyUpdateAndRestart()
    {
        if (_updateManager == null || _pendingUpdate == null) return;

        _logger.LogInformation("Güncelleme uygulanıyor, uygulama yeniden başlatılacak...");
        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }
}
