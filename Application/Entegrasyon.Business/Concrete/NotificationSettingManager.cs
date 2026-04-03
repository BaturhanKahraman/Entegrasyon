using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Notifications;
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class NotificationSettingManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    INotificationManager notificationManager) : INotificationSettingManager
{
    public async Task<NotificationSetting> GetOrCreateForUserAsync(Guid userId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var setting = await dbContext.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (setting is not null) return setting;

        setting = new NotificationSetting
        {
            UserId = userId,
            EnableOrderNotifications = true,
            EnableStockAlerts = true,
            EnableMarketplaceSyncNotifications = true,
            EnableEmailNotifications = false,
            ShowSnackbar = true,
            SnackbarDurationSeconds = 5
        };

        dbContext.NotificationSettings.Add(setting);
        await dbContext.SaveChangesAsync();
        return setting;
    }

    public async Task UpdateAsync(Guid userId, UpdateNotificationSettingDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var setting = await dbContext.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (setting is null)
        {
            setting = new NotificationSetting { UserId = userId };
            dbContext.NotificationSettings.Add(setting);
        }

        setting.EnableOrderNotifications = dto.EnableOrderNotifications;
        setting.EnableStockAlerts = dto.EnableStockAlerts;
        setting.EnableMarketplaceSyncNotifications = dto.EnableMarketplaceSyncNotifications;
        setting.EnableEmailNotifications = dto.EnableEmailNotifications;
        setting.ShowSnackbar = dto.ShowSnackbar;
        setting.SnackbarDurationSeconds = dto.SnackbarDurationSeconds;

        await dbContext.SaveChangesAsync();
    }

    public async Task SendTestNotificationAsync(Guid userId)
    {
        await notificationManager.SendNotification(
            header: "Deneme Bildirimi",
            content: "Bu bir deneme bildirimidir. Bildirimler düzgün çalışıyor!",
            severity: NotificationSeverity.Success,
            category: NotificationCategory.Sistem,
            userIds: [userId]);
    }
}
