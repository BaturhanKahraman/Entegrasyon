using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

public class TrendyolCategoryImportBackgroundService : BackgroundService
{
    private readonly EventChannel<CategoryImportRequestedEvent> _importRequestedChannel;
    private readonly EventChannel<CategoryImportCompletedEvent> _importCompletedChannel;
    private readonly EventChannel<NotificationEvent> _notificationChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrendyolCategoryImportBackgroundService> _logger;

    public TrendyolCategoryImportBackgroundService(
        EventChannel<CategoryImportRequestedEvent> importRequestedChannel,
        EventChannel<CategoryImportCompletedEvent> importCompletedChannel,
        EventChannel<NotificationEvent> notificationChannel,
        IServiceScopeFactory scopeFactory,
        ILogger<TrendyolCategoryImportBackgroundService> logger)
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
                var trendyolImporter = scope.ServiceProvider.GetRequiredService<TrendyolCategoryImporter>();
                var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();

                await SendNotificationAsync(notificationManager,
                    "Trendyol kategori içe aktarma işlemi başladı",
                    $"{importEvent.Categories.Count()} kategori içe aktarılıyor...",
                    [importEvent.UserId]);

                var result = await trendyolImporter.ImportCategoriesAsync(importEvent.Categories, stoppingToken);

                await _importCompletedChannel.PublishAsync(new CategoryImportCompletedEvent(
                    importEvent.MarketplaceName,
                    importEvent.Categories.Count(),
                    result.Success,
                    result.Success ? null : result.Message,
                    importEvent.UserId));

                var header = result.Success
                    ? "Trendyol kategori içe aktarma tamamlandı"
                    : "Trendyol kategori içe aktarma hatası";
                var content = result.Success
                    ? $"{importEvent.Categories.Count()} kategori başarıyla içe aktarıldı."
                    : $"İçe aktarma başarısız: {result.Message}";

                await SendNotificationAsync(notificationManager, header, content, [importEvent.UserId]);

                _logger.LogInformation("Category import completed for user {UserId}: {Success}",
                    importEvent.UserId, result.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Category import failed for user {UserId}", importEvent.UserId);
                using var scope = _scopeFactory.CreateScope();
                var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();
                await SendNotificationAsync(notificationManager,
                    "Trendyol kategori içe aktarma hatası",
                    $"Beklenmeyen hata: {ex.Message}",
                    [importEvent.UserId]);
            }
        }
    }

    private async Task SendNotificationAsync(INotificationManager notificationManager, string header, string content, IEnumerable<Guid> userIds)
    {
        try
        {
            var notification = new Notification
            {
                Header = header,
                Content = content,
                Users = new List<ApplicationUser>(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            await notificationManager.SendNotification(notification, [SenderType.RealTime]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
        }
    }
}
