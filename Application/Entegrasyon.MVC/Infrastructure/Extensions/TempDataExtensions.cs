using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

public static class TempDataExtensions
{
    /// <summary>
    /// Belirtilen mesajı "success" türünde bir toast mesajı olarak TempData'ya ekler.
    /// </summary>
    /// <param name="tempData"></param>
    /// <param name="message">Toast mesajı olarak gösterilecek metin.</param>
    public static void SetSuccess(this ITempDataDictionary tempData, string message)
        => tempData["_toast"] = JsonSerializer.Serialize(new ToastMessage("success", message));

    public static void SetError(this ITempDataDictionary tempData, string message)
        => tempData["_toast"] = JsonSerializer.Serialize(new ToastMessage("danger", message));

    public static void SetWarning(this ITempDataDictionary tempData, string message)
        => tempData["_toast"] = JsonSerializer.Serialize(new ToastMessage("warning", message));

    public static ToastMessage? GetToast(this ITempDataDictionary tempData)
        => tempData["_toast"] is string json
            ? JsonSerializer.Deserialize<ToastMessage>(json)
            : null;

    public static void AddAlert(this ITempDataDictionary tempData, string type, string message)
    {
        var alerts = tempData.GetAlerts();
        alerts.Add(new ToastMessage(type, message));
        tempData["_alerts"] = JsonSerializer.Serialize(alerts);
    }

    public static List<ToastMessage> GetAlerts(this ITempDataDictionary tempData)
        => tempData.Peek("_alerts") is string json
            ? JsonSerializer.Deserialize<List<ToastMessage>>(json) ?? []
            : [];
}

public record ToastMessage(string Type, string Text);
