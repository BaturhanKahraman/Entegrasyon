using Entegrasyon.Business.Channels.Events.Notifications;

namespace Entegrasyon.Business.Notifications;

/// <summary>
/// SignalR veya başka bir real-time transport eklenecekse bu interface implement edilir.
/// Şu an InProcessNotificationDeliveryService bu interface'i implement eder.
/// </summary>
public interface INotificationChannel
{
    Task SendAsync(NotificationEvent evt, IEnumerable<Guid> userIds);
}
