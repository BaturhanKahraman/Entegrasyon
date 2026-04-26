namespace Entegrasyon.Business.Notifications.WebPush;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush:Admin";

    public string VapidSubject { get; set; } = "mailto:admin@entegrasyon.tr";
    public string VapidPublicKey { get; set; } = "";
    public string VapidPrivateKey { get; set; } = "";
}
