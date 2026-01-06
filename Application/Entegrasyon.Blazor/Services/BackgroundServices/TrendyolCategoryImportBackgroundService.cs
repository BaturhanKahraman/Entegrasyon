using Entegrasyon.Blazor.Services.Channels;
using Entegrasyon.Blazor.Services.Channels.Events;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Blazor.Services.BackgroundServices;

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

                // Create a scope to resolve scoped services
                using var scope = _scopeFactory.CreateScope();
                var trendyolImporter = scope.ServiceProvider.GetRequiredService<TrendyolCategoryImporter>();
                var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();

                // Send notification that import started
                await SendNotificationAsync(
                    notificationManager,
                    $"Trendyol kategori içe aktarma işlemi başladı",
                    $"{importEvent.Categories.Count()} kategori içe aktarılıyor...",
                    new[] { importEvent.UserId });

                // Perform the import
                var result = await trendyolImporter.ImportCategoriesAsync(importEvent.Categories, stoppingToken);

                // Send completion event
                var completedEvent = new CategoryImportCompletedEvent(
                    importEvent.MarketplaceName,
                    importEvent.Categories.Count(),
                    result.Success,
                    result.Success ? null : result.Message,
                    importEvent.UserId);

                await _importCompletedChannel.PublishAsync(completedEvent);

                // Send completion notification
                if (result.Success)
                {
                    await SendNotificationAsync(
                        notificationManager,
                        "Trendyol kategori içe aktarma tamamlandı",
                        $"{importEvent.Categories.Count()} kategori başarıyla içe aktarıldı.",
                        new[] { importEvent.UserId });
                }
                else
                {
                    await SendNotificationAsync(
                        notificationManager,
                        "Trendyol kategori içe aktarma hatası",
                        $"İçe aktarma başarısız: {result.Message}",
                        new[] { importEvent.UserId });
                }

                _logger.LogInformation("Category import completed for user {UserId}: {Success}",
                    importEvent.UserId, result.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Category import failed for user {UserId}", importEvent.UserId);

                // Create a scope for error notification
                using var scope = _scopeFactory.CreateScope();
                var notificationManager = scope.ServiceProvider.GetRequiredService<INotificationManager>();

                // Send error notification
                await SendNotificationAsync(
                    notificationManager,
                    "Trendyol kategori içe aktarma hatası",
                    $"Beklenmeyen hata: {ex.Message}",
                    new[] { importEvent.UserId });
            }
        }
    }

    private async Task SendNotificationAsync(INotificationManager notificationManager, string header, string content, IEnumerable<Guid> userIds)
    {
        // Notification'ı Users collection'ı boş şekilde oluştur
        // (Users'ları kurgulamak veritabanında olmayan user referans'larına neden oluyor)
        var notification = new Notification
        {
            Header = header,
            Content = content,
            Users = new List<ApplicationUser>(), // Boş - SignalR için kullanıcılara bildirim gönderilen sırada handle ediliyor
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Notification'ı kaydet (Users collection boş olduğu için no foreign key issues)
        await notificationManager.SendNotification(notification, new[] { SenderType.RealTime });
    }
}
