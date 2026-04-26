namespace Entegrasyon.Business.Notifications;

public enum SenderType
{
    SignalR = 0,  // Phase 6'da kaldırılacak
    Email = 1,
    Sms = 2,
    Sse = 3,
    WebPushAdmin = 4
}
