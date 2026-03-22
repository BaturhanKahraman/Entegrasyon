using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

public class CategoryImportBackgroundService : BackgroundService
{
    private readonly EventChannel<CategoryImportRequestedEvent> _importRequestedChannel;
    private readonly EventChannel<CategoryImportCompletedEvent> _importCompletedChannel;
    private readonly EventChannel<NotificationEvent> _notificationChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CategoryImportBackgroundService> _logger;

    public CategoryImportBackgroundService(
        EventChannel<CategoryImportRequestedEvent> importRequestedChannel,
        EventChannel<CategoryImportCompletedEvent> importCompletedChannel,
        EventChannel<NotificationEvent> notificationChannel,
        IServiceScopeFactory scopeFactory,
        ILogger<CategoryImportBackgroundService> logger)
    {
        _importRequestedChannel = importRequestedChannel;
        _importCompletedChannel = importCompletedChannel;
        _notificationChannel = notificationChannel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var importEvent in _importRequestedChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Starting category import for user {UserId}, {Count} categories",
                    importEvent.UserId, importEvent.Categories.Count());

                using var scope = _scopeFactory.CreateScope();

                // Route by MarketplaceName
                BaseCategoryImporterService importer = importEvent.MarketplaceName switch
                {
                    "Trendyol" => scope.ServiceProvider.GetRequiredService<TrendyolCategoryImporter>(),
                    "N11" => scope.ServiceProvider.GetRequiredService<N11CategoryImporter>(),
                    "Hepsiburada" => scope.ServiceProvider.GetRequiredService<HepsiburadaCategoryImporter>(),
                    _ => throw new InvalidOperationException($"Bilinmeyen pazaryeri: {importEvent.MarketplaceName}")
                };

                var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();

                try
                {
                    await notificationManager.SendNotification(
                        header: $"{importEvent.MarketplaceName} kategori içe aktarma işlemi başladı",
                        content: $"{importEvent.Categories.Count()} kategori içe aktarılıyor...",
                        severity: NotificationSeverity.Info,
                        category: NotificationCategory.Pazaryeri,
                        userIds: [importEvent.UserId]);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send start notification");
                }

                var result = await importer.ImportCategoriesAsync(importEvent.Categories, stoppingToken);

                await _importCompletedChannel.PublishAsync(new CategoryImportCompletedEvent(
                    importEvent.MarketplaceName,
                    importEvent.Categories.Count(),
                    result.Success,
                    result.Success ? null : result.Message,
                    importEvent.UserId));

                var header = result.Success
                    ? $"{importEvent.MarketplaceName} kategori içe aktarma tamamlandı"
                    : $"{importEvent.MarketplaceName} kategori içe aktarma hatası";
                var content = result.Success
                    ? $"{importEvent.Categories.Count()} kategori başarıyla içe aktarıldı."
                    : $"İçe aktarma başarısız: {result.Message}";

                try
                {
                    await notificationManager.SendNotification(
                        header: header,
                        content: content,
                        severity: result.Success ? NotificationSeverity.Success : NotificationSeverity.Error,
                        category: NotificationCategory.Pazaryeri,
                        userIds: [importEvent.UserId],
                        actionUrl: result.Success ? "/marketplace/sync" : null);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send result notification");
                }

                _logger.LogInformation("Category import completed for user {UserId}: {Success}",
                    importEvent.UserId, result.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Category import failed for user {UserId}", importEvent.UserId);
                using var scope = _scopeFactory.CreateScope();
                var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();
                try
                {
                    await notificationManager.SendNotification(
                        header: $"{importEvent.MarketplaceName} kategori içe aktarma hatası",
                        content: $"Beklenmeyen hata: {ex.Message}",
                        severity: NotificationSeverity.Error,
                        category: NotificationCategory.Pazaryeri,
                        userIds: [importEvent.UserId]);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send error notification");
                }
            }
        }
    }
}
