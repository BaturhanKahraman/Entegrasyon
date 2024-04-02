using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Concrete;

#pragma warning disable CS9113 // Parameter is unread.
public sealed class NotificationManager(IntegrationDbContext context)
#pragma warning restore CS9113 // Parameter is unread.
{
    //Bir bildirimin kime gönderileceği,
    //başlığı var,içeriği var, oluşturulduğu zamanı var,okunma zamanı var
    //konusu var, gönderileceği cihaz var,
    //signalr ?
    public async Task SendNotification(Notification notification)
    {
        await Task.Yield();
    }

}