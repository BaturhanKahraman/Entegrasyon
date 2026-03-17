using Entegrasyon.Entity.Dtos.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Abstract;

public interface INotificationSettingManager
{
    Task<NotificationSetting> GetOrCreateForUserAsync(Guid userId);
    Task UpdateAsync(Guid userId, UpdateNotificationSettingDto dto);
    Task SendTestNotificationAsync(Guid userId);
}
