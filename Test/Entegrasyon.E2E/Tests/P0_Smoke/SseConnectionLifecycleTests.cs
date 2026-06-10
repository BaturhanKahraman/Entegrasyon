using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.Tests.P0_Smoke;

/// <summary>
/// Bildirim SSE (EventSource) bağlantısının yaşam döngüsünü doğrular.
///
/// Kök sorun: Dev/prod HTTP/1.1 üzerinden servis edilir; tarayıcı host başına ~6
/// eşzamanlı bağlantıyla sınırlıdır. Her admin sayfası kalıcı bir
/// EventSource('/events/notifications') açar. Bağlantı sayfadan ayrılırken
/// kapatılmazsa, hızlı gezinmede bağlantılar birikir, host bağlantı havuzu tükenir
/// ve sonraki istekler (doküman/asset/XHR) kuyruğa girip takılır = "geç yükleme".
///
/// Fix: notifications-client.js EventSource'u pagehide'da close() eder ve tek
/// bağlantı garantisi sağlar.
/// </summary>
[TestFixture, Order(2)]
public class SseConnectionLifecycleTests : E2ETestBase
{
    /// <summary>
    /// EventSource'u sarmalayıp açılan/kapanan örnekleri window üzerinde sayan init script.
    /// Sayfa scriptlerinden ÖNCE çalışır, bu yüzden notifications-client.js'in açtığı
    /// gerçek EventSource'u yakalar.
    /// </summary>
    private const string EventSourceTrackerScript = """
        (() => {
            const Native = window.EventSource;
            if (!Native || Native.__tracked) return;
            const counters = { opened: 0, closed: 0 };
            window.__sseCounters = counters;
            function Tracked(url, opts) {
                const es = new Native(url, opts);
                if (String(url).includes('/events/notifications')) {
                    counters.opened++;
                    const origClose = es.close.bind(es);
                    es.close = function () { counters.closed++; return origClose(); };
                }
                return es;
            }
            Tracked.prototype = Native.prototype;
            Tracked.CONNECTING = Native.CONNECTING;
            Tracked.OPEN = Native.OPEN;
            Tracked.CLOSED = Native.CLOSED;
            Tracked.__tracked = true;
            window.EventSource = Tracked;
        })();
        """;

    [SetUp]
    public async Task LoginBeforeTest()
    {
        await LoginAsAdminAsync();
    }

    /// <summary>
    /// Admin sayfasından ayrılınca SSE bağlantısı close() ile serbest bırakılmalı.
    /// (pagehide handler'ı). Aksi halde bağlantı birikir.
    /// </summary>
    [Test]
    public async Task Sse_IsClosed_OnNavigationAway()
    {
        await Page.AddInitScriptAsync(EventSourceTrackerScript);

        await Page.GotoAsync($"{BaseUrl}/discounts");
        await Page.WaitForFunctionAsync("() => window.__sseCounters && window.__sseCounters.opened >= 1");

        var openedBefore = await Page.EvaluateAsync<int>("() => window.__sseCounters.opened");
        Assert.That(openedBefore, Is.GreaterThanOrEqualTo(1), "Admin sayfasında SSE bağlantısı açılmalı.");

        // pagehide'ı tetikle: aynı sayfa içinde close çağrıldığını ölç.
        var closedOnHide = await Page.EvaluateAsync<int>("""
            async () => {
                window.dispatchEvent(new Event('pagehide'));
                await new Promise(r => setTimeout(r, 50));
                return window.__sseCounters.closed;
            }
            """);

        Assert.That(closedOnHide, Is.GreaterThanOrEqualTo(1),
            "Sayfadan ayrılırken (pagehide) SSE bağlantısı kapatılmadı — bağlantı sızıyor.");
    }

    /// <summary>
    /// Bir admin sayfası en fazla TEK SSE bağlantısı açmalı (çift bağlantı garantisi).
    /// </summary>
    [Test]
    public async Task Sse_OpensExactlyOnce_PerPage()
    {
        await Page.AddInitScriptAsync(EventSourceTrackerScript);

        await Page.GotoAsync($"{BaseUrl}/marketplace/sync");
        await Page.WaitForFunctionAsync("() => window.__sseCounters && window.__sseCounters.opened >= 1");
        // Kısa süre bekle — yanlışlıkla ikinci bağlantı açılmadığından emin ol.
        await Task.Delay(800);

        var opened = await Page.EvaluateAsync<int>("() => window.__sseCounters.opened");
        Assert.That(opened, Is.EqualTo(1),
            $"Sayfa başına tam olarak 1 SSE bağlantısı beklenir, {opened} açıldı.");
    }
}
