using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

public class CategoryImportBackgroundService(
    EventChannel<CategoryImportRequestedEvent> importRequestedChannel,
    EventChannel<CategoryImportCompletedEvent> importCompletedChannel,
    EventChannel<NotificationEvent> notificationChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<CategoryImportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var importEvent in importRequestedChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation("Starting category import for user {UserId}, {Count} categories, TenantId={TenantId}",
                    importEvent.UserId, importEvent.Categories.Count(), importEvent.TenantId);

                using var scope = scopeFactory.CreateScope();

                // Tenant context initialize
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
                var tenant = await tenantRegistry.GetByIdAsync(importEvent.TenantId);
                if (tenant is null || !tenant.IsActive)
                {
                    logger.LogWarning("{Service}: Tenant {TenantId} not found or inactive, skipping event",
                        nameof(CategoryImportBackgroundService), importEvent.TenantId);
                    continue;
                }
                tenantContext.Initialize(tenant);

                // Route by MarketplaceName
                BaseCategoryImporterService importer = importEvent.MarketplaceName switch
                {
                    "Trendyol" => scope.ServiceProvider.GetRequiredService<TrendyolCategoryImporter>(),
                    "N11" => scope.ServiceProvider.GetRequiredService<N11CategoryImporter>(),
                    "Hepsiburada" => scope.ServiceProvider.GetRequiredService<HepsiburadaCategoryImporter>(),
                    "Pazarama" => scope.ServiceProvider.GetRequiredService<PazaramaCategoryImporter>(),
                    "PttAVM" => scope.ServiceProvider.GetRequiredService<PttavmCategoryImporter>(),
                    "Temu" => scope.ServiceProvider.GetRequiredService<TemuCategoryImporter>(),
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
                    logger.LogError(notifEx, "Failed to send start notification");
                }

                var result = await importer.ImportCategoriesAsync(importEvent.Categories, stoppingToken);

                await importCompletedChannel.PublishAsync(new CategoryImportCompletedEvent(
                    importEvent.MarketplaceName,
                    importEvent.Categories.Count(),
                    result.Success,
                    result.Success ? null : result.Message,
                    importEvent.UserId)
                {
                    TenantId = importEvent.TenantId
                });

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
                    logger.LogError(notifEx, "Failed to send result notification");
                }

                logger.LogInformation("Category import completed for user {UserId}: {Success}",
                    importEvent.UserId, result.Success);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Category import failed for user {UserId}, TenantId={TenantId}",
                    importEvent.UserId, importEvent.TenantId);
                using var scope = scopeFactory.CreateScope();

                // Tenant context initialize for error notification scope
                try
                {
                    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                    var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
                    var tenant = await tenantRegistry.GetByIdAsync(importEvent.TenantId);
                    if (tenant is not null && tenant.IsActive)
                        tenantContext.Initialize(tenant);
                }
                catch (Exception tenantEx)
                {
                    logger.LogError(tenantEx, "Failed to initialize tenant context for error notification");
                }

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
                    logger.LogError(notifEx, "Failed to send error notification");
                }
            }
        }
    }
}
