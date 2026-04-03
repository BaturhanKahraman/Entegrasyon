using System.Text.Json;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

/// <summary>
/// HTMX request detection ve response header yardımcıları.
/// </summary>
public static class HtmxExtensions
{
    // ── Request Detection ────────────────────────────────────────────────

    public static bool IsHtmx(this HttpRequest request)
        => request.Headers.ContainsKey("HX-Request");

    public static bool IsHtmxBoosted(this HttpRequest request)
        => request.Headers.ContainsKey("HX-Boosted");

    public static string? HtmxTarget(this HttpRequest request)
        => request.Headers["HX-Target"].FirstOrDefault();

    public static string? HtmxTriggerName(this HttpRequest request)
        => request.Headers["HX-Trigger-Name"].FirstOrDefault();

    // ── Response Headers ─────────────────────────────────────────────────

    public static void HtmxRedirect(this HttpResponse response, string url)
        => response.Headers.Append("HX-Redirect", url);

    public static void HtmxRefresh(this HttpResponse response)
        => response.Headers.Append("HX-Refresh", "true");

    public static void HtmxTrigger(this HttpResponse response, string eventName)
        => response.Headers.Append("HX-Trigger", eventName);

    public static void HtmxTriggerWithData(this HttpResponse response,
        string eventName, object data)
    {
        var json = JsonSerializer.Serialize(
            new Dictionary<string, object> { [eventName] = data });
        response.Headers.Append("HX-Trigger", json);
    }

    public static void HtmxReswap(this HttpResponse response, string strategy)
        => response.Headers.Append("HX-Reswap", strategy);

    public static void HtmxRetarget(this HttpResponse response, string selector)
        => response.Headers.Append("HX-Retarget", selector);

    public static void HtmxPushUrl(this HttpResponse response, string url)
        => response.Headers.Append("HX-Push-Url", url);
}
