using System.Net.Http.Json;
using System.Text.Json;
using Entegrasyon.Desktop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Desktop.Services;

/// <summary>
/// Handles bidirectional sync between local SQLite and remote server.
/// Pull: server products/stock -> SQLite
/// Push: offline sales -> server (queue-based outbox pattern, FIFO)
/// Conflict: first-sync-wins (stock can go negative, corrected later)
/// </summary>
public class SyncService : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SettingsService _settingsService;
    private readonly DeviceCredentialStore _credentialStore;
    private readonly ILogger<SyncService> _logger;
    private readonly HttpClient _httpClient;
    private Timer? _syncTimer;
    private Timer? _connectivityTimer;
    private bool _isSyncing;
    private bool _isOnline;

    public bool IsOnline => _isOnline;
    public DateTimeOffset? LastSyncTime { get; private set; }
    public int PendingCount { get; private set; }
    public string? LastError { get; private set; }

    public event Action<bool>? OnConnectivityChanged;
    public event Action<SyncResult>? OnSyncCompleted;

    public SyncService(
        IServiceProvider serviceProvider,
        SettingsService settingsService,
        DeviceCredentialStore credentialStore,
        ILogger<SyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        _credentialStore = credentialStore;
        _logger = logger;
        _httpClient = new HttpClient();

        // Start connectivity monitoring (every 10 seconds)
        _connectivityTimer = new Timer(async _ => await CheckConnectivity(), null,
            TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    private async Task CheckConnectivity()
    {
        bool wasOnline = _isOnline;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            await http.GetAsync("https://www.google.com/generate_204");
            _isOnline = true;
        }
        catch
        {
            _isOnline = false;
        }

        if (wasOnline != _isOnline)
        {
            OnConnectivityChanged?.Invoke(_isOnline);

            if (_isOnline)
            {
                _logger.LogInformation("Connection restored, triggering sync");
                await FullSyncAsync();
            }
        }
    }

    /// <summary>
    /// Start periodic auto-sync timer.
    /// </summary>
    public void StartAutoSync()
    {
        var interval = TimeSpan.FromSeconds(_settingsService.Settings.SyncIntervalSeconds);
        _syncTimer = new Timer(async _ => await TryAutoSync(), null, interval, interval);
        _logger.LogInformation("Auto-sync started with interval {Interval}s", interval.TotalSeconds);
    }

    public void StopAutoSync()
    {
        _syncTimer?.Dispose();
        _syncTimer = null;
    }

    private async Task TryAutoSync()
    {
        if (!IsOnline || _isSyncing) return;

        try
        {
            await FullSyncAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-sync failed");
        }
    }

    /// <summary>
    /// Pull products + stock from server into local SQLite.
    /// </summary>
    public async Task<SyncResult> PullProductsAsync()
    {
        if (!IsOnline)
            return new SyncResult(false, "Offline — sync not possible");

        var settings = _settingsService.Settings;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OfflineDbContext>();

            var since = await db.Products
                .OrderByDescending(p => p.LastSyncedAt)
                .Select(p => p.LastSyncedAt)
                .FirstOrDefaultAsync();

            var url = $"{settings.ServerUrl}/api/sync/catalog?since={since:O}";
            if (!string.IsNullOrEmpty(settings.BranchOfficeId))
                url += $"&branchOfficeId={settings.BranchOfficeId}";

            ConfigureHttpClient(settings);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var products = await response.Content.ReadFromJsonAsync<List<SyncProductDto>>();
            if (products is null || products.Count == 0)
                return new SyncResult(true, "No new products to sync", 0);

            var now = DateTimeOffset.UtcNow;
            foreach (var dto in products)
            {
                var existing = await db.Products.FindAsync(dto.ProductId);
                if (existing is not null)
                {
                    existing.Title = dto.Title;
                    existing.Barcode = dto.Barcode;
                    existing.Size = dto.Size;
                    existing.SalePrice = dto.SalePrice;
                    existing.ListPrice = dto.ListPrice;
                    existing.CostPrice = dto.CostPrice;
                    existing.StockQuantity = dto.StockQuantity;
                    existing.VatRate = dto.VatRate;
                    existing.LastSyncedAt = now;
                }
                else
                {
                    db.Products.Add(new OfflineProduct
                    {
                        ProductId = dto.ProductId,
                        Title = dto.Title,
                        Barcode = dto.Barcode,
                        Size = dto.Size,
                        SalePrice = dto.SalePrice,
                        ListPrice = dto.ListPrice,
                        CostPrice = dto.CostPrice,
                        StockQuantity = dto.StockQuantity,
                        VatRate = dto.VatRate,
                        LastSyncedAt = now
                    });
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Pulled {Count} products from server", products.Count);
            return new SyncResult(true, $"{products.Count} product(s) synced", products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull products failed");
            LastError = ex.Message;
            return new SyncResult(false, $"Pull failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Push offline sales to /api/offline-sales/sync. Tek-batch envelope ile gönderilir;
    /// server idempotency-key bazlı işler. Bearer dev_xxx auth kullanılır.
    /// </summary>
    public async Task<SyncResult> PushSalesAsync()
    {
        if (!IsOnline)
            return new SyncResult(false, "Offline — sync not possible");

        var credentials = _credentialStore.TryLoad();
        if (credentials is null)
            return new SyncResult(false, "Cihaz kayıtlı değil — credentials.json yok.");

        var settings = _settingsService.Settings;
        if (!int.TryParse(settings.BranchOfficeId, out var branchOfficeId))
            return new SyncResult(false, "BranchOfficeId ayarlı değil.");
        if (!Guid.TryParse(settings.SalePersonId, out var salePersonId))
            return new SyncResult(false, "SalePersonId ayarlı değil.");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OfflineDbContext>();

            var pendingSales = await db.Sales
                .Include(s => s.Items)
                .Where(s => !s.IsSynced)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            if (pendingSales.Count == 0)
                return new SyncResult(true, "Senkronize edilecek satış yok.", 0);

            var dtos = pendingSales.Select(s => new OfflineSaleSyncDto(
                IdempotencyKey: s.Id.ToString("N"),
                OccurredAt: s.SaleDate,
                BranchOfficeId: branchOfficeId,
                SalePersonId: salePersonId,
                CustomerId: null,
                GeneralDiscount: 0m,
                PaymentMethod: s.PaymentMethod,
                Items: s.Items.Select(i => new OfflineSaleItemSyncDto(
                    ProductVariantId: i.ProductId,
                    Quantity: i.Quantity,
                    UnitPrice: i.UnitPrice,
                    DiscountPercent: i.DiscountPercent)).ToList()
            )).ToList();

            ConfigureHttpClient(credentials);
            var url = $"{credentials.ServerBaseUrl.TrimEnd('/')}/api/offline-sales/sync";
            var response = await _httpClient.PostAsJsonAsync(url, new { sales = dtos });

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return new SyncResult(false, $"HTTP {response.StatusCode}: {body}");
            }

            var syncResp = await response.Content.ReadFromJsonAsync<SyncResponseDto>();
            if (syncResp is null)
                return new SyncResult(false, "Sunucudan boş yanıt.");

            // Mark synced + record any incident references
            var byKey = pendingSales.ToDictionary(s => s.Id.ToString("N"), s => s);
            var oversoldCount = 0;
            foreach (var r in syncResp.Results)
            {
                if (!byKey.TryGetValue(r.IdempotencyKey, out var sale)) continue;
                if (r.Status == "ok" || r.Status == "oversold" || r.Status == "duplicate")
                {
                    sale.IsSynced = true;
                    sale.SyncedAt = DateTimeOffset.UtcNow;
                    if (r.Status == "oversold") oversoldCount++;
                }
            }
            await db.SaveChangesAsync();
            PendingCount = pendingSales.Count(s => !s.IsSynced);

            var msg = oversoldCount > 0
                ? $"{syncResp.Results.Count} senkronize, {oversoldCount} tanesi oversell — admin paneline incident gitti."
                : $"{syncResp.Results.Count} satış senkronize.";
            _logger.LogInformation("PushSales: {Msg}", msg);
            return new SyncResult(true, msg, syncResp.Results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push sales failed");
            LastError = ex.Message;
            return new SyncResult(false, $"Push failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Full sync: push pending sales first, then pull latest products.
    /// </summary>
    public async Task<SyncResult> FullSyncAsync()
    {
        if (_isSyncing) return new SyncResult(false, "Sync already in progress");
        _isSyncing = true;

        try
        {
            var pushResult = await PushSalesAsync();
            var pullResult = await PullProductsAsync();

            LastSyncTime = DateTimeOffset.UtcNow;
            LastError = null;

            var result = new SyncResult(
                pushResult.Success && pullResult.Success,
                $"Push: {pushResult.Message} | Pull: {pullResult.Message}",
                (pushResult.AffectedCount ?? 0) + (pullResult.AffectedCount ?? 0));

            OnSyncCompleted?.Invoke(result);
            return result;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Get count of pending (unsynced) queue items.
    /// </summary>
    public async Task<int> GetPendingCountAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OfflineDbContext>();
        PendingCount = await db.SyncQueue.CountAsync(q => !q.IsSynced);
        return PendingCount;
    }

    private void ConfigureHttpClient(PosSettings settings)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        // Catalog pull yine X-Api-Key kullanıyor (eski kontrat); offline-sales/sync
        // ayrıca Bearer auth ile çağrılır (ConfigureHttpClient(DeviceCredentials)).
        if (!string.IsNullOrEmpty(settings.ApiKey))
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
    }

    private void ConfigureHttpClient(DeviceCredentials credentials)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.DeviceApiKey);
    }

    public void Dispose()
    {
        _syncTimer?.Dispose();
        _connectivityTimer?.Dispose();
        _httpClient.Dispose();
    }
}

public record SyncResult(bool Success, string Message, int? AffectedCount = null);

public record OfflineSaleSyncDto(
    string IdempotencyKey,
    DateTimeOffset OccurredAt,
    int BranchOfficeId,
    Guid SalePersonId,
    int? CustomerId,
    decimal GeneralDiscount,
    string PaymentMethod,
    List<OfflineSaleItemSyncDto> Items);

public record OfflineSaleItemSyncDto(
    Guid ProductVariantId,
    int Quantity,
    decimal UnitPrice,
    double DiscountPercent);

public record SyncResponseDto(List<SyncResultDto> Results);

public record SyncResultDto(string IdempotencyKey, string Status, Guid? SaleId, int? IncidentId, string? Message);

/// <summary>
/// DTO for product data received from server sync endpoint.
/// </summary>
public record SyncProductDto(
    Guid ProductId,
    string Title,
    string Barcode,
    string? Size,
    decimal SalePrice,
    decimal ListPrice,
    decimal CostPrice,
    int StockQuantity,
    decimal VatRate);
