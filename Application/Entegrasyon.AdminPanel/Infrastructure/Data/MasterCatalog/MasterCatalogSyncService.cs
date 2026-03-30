using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.AdminPanel.Infrastructure.Data.MasterCatalog;

/// <summary>
/// Günlük Trendyol API sync background service.
/// Mevcut MarketplaceReference tablosunu günceller ve delta'ları MasterCategory'e yansıtır.
/// Production'da çalışır; development'ta devre dışıdır (appsettings'den kontrol edilebilir).
/// </summary>
public class MasterCatalogSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<MasterCatalogSyncService> logger) : BackgroundService
{
    private const int TrendyolMarketplaceId = 1;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5); // Startup sonrası bekle

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MasterCatalogSyncService başlatıldı.");

        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Master catalog sync sırasında hata oluştu.");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminPanelDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        logger.LogInformation("Trendyol kategori sync başlıyor...");

        try
        {
            var httpClient = httpClientFactory.CreateClient("TrendyolPublic");
            var categories = await httpClient.GetFromJsonAsync<TrendyolCategoryResponse>(
                "https://api.trendyol.com/sapigw/product-categories", ct);

            if (categories?.Categories is null || categories.Categories.Count == 0)
            {
                logger.LogWarning("Trendyol API'den boş kategori yanıtı alındı.");
                return;
            }

            var counters = new SyncCounters();
            var syncTime = DateTimeOffset.UtcNow;

            // Tüm harici ID'leri topla
            var allExternalIds = new HashSet<string>();
            CollectExternalIds(categories.Categories, allExternalIds);

            // Mevcut referansları çek
            var existingRefs = await dbContext.MarketplaceReferences
                .Where(r => r.MarketplaceId == TrendyolMarketplaceId && r.EntityType == MarketplaceEntityType.Category)
                .ToListAsync(ct);

            var existingDict = existingRefs.ToDictionary(r => r.ExternalId);

            // Yeni veya değişmiş kayıtları işle
            await ProcessCategories(dbContext, categories.Categories, null, existingDict, syncTime, counters, ct);

            // Kaybolmuş kategorileri deactivate et
            foreach (var existing in existingRefs.Where(r => r.IsActive && !allExternalIds.Contains(r.ExternalId)))
            {
                existing.IsActive = false;
                counters.Deactivated++;
            }

            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation(
                "Trendyol sync tamamlandı: {Added} eklendi, {Updated} güncellendi, {Deactivated} deactivate edildi.",
                counters.Added, counters.Updated, counters.Deactivated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trendyol API çağrısı sırasında hata oluştu.");
        }
    }

    private async Task ProcessCategories(
        AdminPanelDbContext dbContext,
        List<TrendyolCategoryItem> items,
        string? parentExternalId,
        Dictionary<string, MarketplaceReference> existingDict,
        DateTimeOffset syncTime,
        SyncCounters counters,
        CancellationToken ct)
    {
        foreach (var item in items)
        {
            var externalId = item.Id.ToString();

            if (existingDict.TryGetValue(externalId, out var existing))
            {
                if (existing.Name != item.Name)
                {
                    existing.Name = item.Name;
                    existing.LastSyncedAt = syncTime;
                    existing.UpdatedAt = DateTime.UtcNow;
                    counters.Updated++;
                }
                else
                {
                    existing.LastSyncedAt = syncTime;
                }
                existing.IsActive = true;
            }
            else
            {
                var newRef = new MarketplaceReference
                {
                    MarketplaceId = TrendyolMarketplaceId,
                    EntityType = MarketplaceEntityType.Category,
                    ExternalId = externalId,
                    Name = item.Name,
                    ParentExternalId = parentExternalId,
                    LastSyncedAt = syncTime,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await dbContext.MarketplaceReferences.AddAsync(newRef, ct);
                counters.Added++;
            }

            if (item.SubCategories is { Count: > 0 })
                await ProcessCategories(dbContext, item.SubCategories, externalId, existingDict, syncTime, counters, ct);
        }
    }

    private sealed class SyncCounters
    {
        public int Added { get; set; }
        public int Updated { get; set; }
        public int Deactivated { get; set; }
    }

    private static void CollectExternalIds(List<TrendyolCategoryItem> items, HashSet<string> result)
    {
        foreach (var item in items)
        {
            result.Add(item.Id.ToString());
            if (item.SubCategories is { Count: > 0 })
                CollectExternalIds(item.SubCategories, result);
        }
    }
}

// ── Trendyol API Response DTOs ─────────────────────────────────────────────

internal class TrendyolCategoryResponse
{
    public List<TrendyolCategoryItem> Categories { get; set; } = [];
}

internal class TrendyolCategoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TrendyolCategoryItem>? SubCategories { get; set; }
}
