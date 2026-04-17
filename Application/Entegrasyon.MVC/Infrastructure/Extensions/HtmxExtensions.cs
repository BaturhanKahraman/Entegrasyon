using System.Text.Json;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

/// <summary>
/// HTMX request detection ve response header yardımcıları.
/// </summary>
public static class HtmxExtensions
{
    // ── Request Detection ────────────────────────────────────────────────
    /// <summary>
    /// HTTP isteğinin HTMX tarafından yapılıp yapılmadığını kontrol eder. HTMX istekleri, "HX-Request" başlığını içerir.
    /// Bu yöntem, bir isteğin HTMX tarafından yapılıp yapılmadığını belirlemek için kullanılabilir.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static bool IsHtmx(this HttpRequest request)
        => request.Headers.ContainsKey("HX-Request");
    /// <summary>
    /// HTTP isteğinin HTMX tarafından "boosted" (güçlendirilmiş) bir istek olup olmadığını kontrol eder.
    /// HTMX, normal bir bağlantıyı veya formu HTMX isteğine dönüştürmek için "boost" özelliğini kullanır.
    /// Bu tür istekler, "HX-Boosted" başlığını içerir.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static bool IsHtmxBoosted(this HttpRequest request)
        => request.Headers.ContainsKey("HX-Boosted");
    /// <summary>
    /// HTTP isteğinin HTMX tarafından yapıldığını ve belirli bir hedefe yönelik olduğunu kontrol eder.
    /// HTMX istekleri, "HX-Target" başlığını içerir.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string? HtmxTarget(this HttpRequest request)
        => request.Headers["HX-Target"].FirstOrDefault();
    /// <summary>
    /// HTTP isteğinin HTMX tarafından yapıldığını ve belirli bir tetikleyici tarafından tetiklendiğini kontrol eder.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string? HtmxTriggerName(this HttpRequest request)
        => request.Headers["HX-Trigger-Name"].FirstOrDefault();

    // ── Response Headers ─────────────────────────────────────────────────
    /// <summary>
    /// HTTP yanıtına HTMX yönlendirme başlığı ekler. Bu, istemcinin belirtilen URL'ye yönlendirilmesini sağlar.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="url">Yönlendirme yapılacak URL.</param>
    public static void HtmxRedirect(this HttpResponse response, string url)
        => response.Headers.Append("HX-Redirect", url);

    /// <summary>
    /// HTTP yanıtına HTMX yenileme başlığı ekler. Bu, istemcinin sayfayı yeniden yüklemesini sağlar.
    /// </summary>
    /// <param name="response"></param>
    public static void HtmxRefresh(this HttpResponse response)
        => response.Headers.Append("HX-Refresh", "true");

    /// <summary>
    /// HTTP yanıtına HTMX tetikleme başlığı ekler. Bu, istemcinin belirtilen olayı tetiklemesini sağlar.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="eventName">Tetiklenecek olayın adı.</param>
    public static void HtmxTrigger(this HttpResponse response, string eventName)
        => response.Headers.Append("HX-Trigger", eventName);

    /// <summary>
    /// HTTP yanıtına HTMX tetikleme başlığı ekler ve veri ile birlikte gönderir. Bu, istemcinin belirtilen olayı tetiklemesini sağlar.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="eventName">Tetiklenecek olayın adı.</param>
    /// <param name="data">Olay ile birlikte gönderilecek veri.</param>
    public static void HtmxTriggerWithData(this HttpResponse response,
        string eventName, object data)
    {
        var json = JsonSerializer.Serialize(
            new Dictionary<string, object> { [eventName] = data });
        response.Headers.Append("HX-Trigger", json);
    }
    /// <summary>
    /// HTTP yanıtına HTMX yeniden yerleştirme başlığı ekler. Bu, istemcinin belirtilen stratejiye göre içeriği yeniden yerleştirmesini sağlar.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="strategy"></param>
    public static void HtmxReswap(this HttpResponse response, string strategy)
        => response.Headers.Append("HX-Reswap", strategy);
    /// <summary>
    /// HTTP yanıtına HTMX yeniden hedefleme başlığı ekler. Bu, istemcinin belirtilen seçiciye göre içeriği yeniden hedeflemesini sağlar.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="selector"></param>
    public static void HtmxRetarget(this HttpResponse response, string selector)
        => response.Headers.Append("HX-Retarget", selector);
    /// <summary>
    /// HTTP yanıtına HTMX URL itme başlığı ekler. Bu, istemcinin tarayıcı geçmişine belirtilen URL'yi eklemesini sağlar.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="url"></param>
    public static void HtmxPushUrl(this HttpResponse response, string url)
        => response.Headers.Append("HX-Push-Url", url);
}
