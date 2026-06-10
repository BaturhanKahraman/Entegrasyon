using System.Net;
using Entegrasyon.E2E.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Entegrasyon.E2E.Tests.P0_Smoke;

/// <summary>
/// HTTP duzeyinde smoke testleri — tarayici olmadan HttpClient ile HTTP status kodu dogrular.
/// Amac: 500 Internal Server Error'lari CI'da aninda yakalamak.
/// Playwright browser testi sifirdan baslar; bu testler HTTP 500'u kesinlikle gormezden gelmez.
///
/// Calisma sekli:
///   1. /auth/login'e POST yaparak oturum acilir ve cookie alinir.
///   2. Cookie'li HttpClient ile her rota tek tek GET edilir.
///   3. HTTP 500 (veya 500 > status >= 500) → test basarisiz.
///   4. HTTP 200/301/302/404 → kabul edilebilir (sayfa mevcut ama veri yoksa 404 normal).
///
/// Bu sinif sadece status kodu denetler; iceriği (HTML render) dogrulamaz.
/// Icerik dogrulamasi NavigationTests'te Playwright ile yapilir.
/// </summary>
[TestFixture, Order(1)]
public class SmokeHttpTests
{
    private string _baseUrl = null!;
    private HttpClient _httpClient = null!;
    private HttpClientHandler _handler = null!;

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.e2e.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    /// <summary>
    /// Test edilecek rotalar — HTTP 500 kesinlikle kabul edilmez.
    /// Her yeni controller eklendiginde bu listeye eklenmeli.
    /// Kritik rotalar basina yorum ile isaretlendi.
    /// </summary>
    private static readonly string[] SmokeRoutes =
    [
        // --- Genel ---
        "/",
        "/products",
        "/categories",
        "/brands",
        "/customers",
        // --- KRITIK: Daha once 500 veren rotalar ---
        "/sales",                      // SaleController — once 500 veriyordu
        "/branch-offices",             // BranchOfficeController — once 500 veriyordu
        "/branch-offices/1",           // Detail — seed ile Id=1 mevcut
        // --- Marketplace ---
        "/marketplace/sync",
        "/marketplace/matching",
        // --- Ayarlar ---
        "/settings/general",
        "/settings/tax",               // KDV oranları
        "/settings/payment-methods",   // Odeme yontemleri
        "/settings/integrations",
        "/settings/shipping",
        // --- Raporlar ---
        "/reports/sales",
        // --- Kullanici & Roller (nav-active kök fix kapsami) ---
        "/users",
        "/roles",                      // nav-active: "roles" leaf-key
        // --- Ayarlar (ek) ---
        "/settings/printing",          // Yazici & Barkod — nav-active: "settings-printing"
        // --- Marketplace (ek) ---
        "/marketplace/commission-rates", // nav-active: "commission-rates"
        // --- Storefront (nav-active leaf-key fix kapsami) ---
        "/storefront/reviews",
        "/storefront/returns",
        "/storefront/sellers",
        "/storefront/campaigns",
        "/storefront/payouts",
        "/loyalty",
        "/picking",
        // --- Diger kritik sayfalar ---
        "/orders",
        "/invoices",
        "/notifications",
        "/logs",
        "/help",
    ];

    [OneTimeSetUp]
    public async Task SetUpHttpClient()
    {
        _baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL")
                   ?? Configuration["E2E:BaseUrl"]
                   ?? "http://localhost:5100";

        _handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            // Dev/test ortaminda self-signed cert olabilir
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Cookie auth ile oturum ac
        await LoginAsync();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    /// <summary>
    /// Her rota icin ayri test metodu — TestCaseSource ile.
    /// Boylece hangi rotanin 500 verdigi dogrudan test adinda gorulur.
    /// </summary>
    [Test]
    [TestCaseSource(nameof(SmokeRoutes))]
    public async Task Route_ReturnsNot500(string route)
    {
        var response = await _httpClient.GetAsync(route);

        // Auth redirect olursa yeniden login yap
        if (response.RequestMessage?.RequestUri?.AbsolutePath.Contains("/auth/login") == true
            && route != "/auth/login")
        {
            await LoginAsync();
            response = await _httpClient.GetAsync(route);
        }

        Assert.That(
            (int)response.StatusCode,
            Is.Not.InRange(500, 599),
            $"Rota '{route}' HTTP {(int)response.StatusCode} dondu — uygulama hatasi.");
    }

    /// <summary>
    /// Tum rotalar ayni anda, tek cagri ile — CI ozetinde toplu goruntulemek icin.
    /// Route_ReturnsNot500 ile ayni rotalar; burada tum hatalari biriktirir.
    /// </summary>
    [Test]
    public async Task AllCriticalRoutes_ReturnNot500_Collectively()
    {
        var failedRoutes = new List<string>();

        foreach (var route in SmokeRoutes)
        {
            try
            {
                var response = await _httpClient.GetAsync(route);

                // Auth redirect kontrol
                if (response.RequestMessage?.RequestUri?.AbsolutePath.Contains("/auth/login") == true
                    && route != "/auth/login")
                {
                    await LoginAsync();
                    response = await _httpClient.GetAsync(route);
                }

                if ((int)response.StatusCode >= 500)
                    failedRoutes.Add($"{route} → HTTP {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                failedRoutes.Add($"{route} → Istisna: {ex.Message}");
            }
        }

        Assert.That(failedRoutes, Is.Empty,
            $"HTTP 500 alan rotalar ({failedRoutes.Count} adet):\n{string.Join('\n', failedRoutes)}");
    }

    // ---------------------------------------------------------------------------
    // Yardimci metodlar
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Form POST ile /auth/login'e giris yapar, cookie'yi HttpClient'a ekler.
    /// </summary>
    private async Task LoginAsync()
    {
        // Login sayfasini once al — CSRF / AntiForgery token gerekirse buradan alinir
        var loginGet = await _httpClient.GetAsync("/auth/login");
        var loginHtml = await loginGet.Content.ReadAsStringAsync();

        // AntiForgery token varsa parse et
        var token = ParseAntiForgeryToken(loginHtml);

        var formData = new List<KeyValuePair<string, string>>
        {
            new("UserName", TestData.TestUsers.AdminUsername),
            new("Password", TestData.TestUsers.AdminPassword),
        };
        if (token != null)
            formData.Add(new("__RequestVerificationToken", token));

        var loginPost = await _httpClient.PostAsync(
            "/auth/login",
            new FormUrlEncodedContent(formData));

        // 302 redirect veya 200 (bazi MVC uygulamalar redirect yapar)
        // CookieContainer otomatik cookie'yi sakladigindan burada ekstra islem gerekmez
        if (loginPost.StatusCode is not HttpStatusCode.Found
                                and not HttpStatusCode.OK
                                and not HttpStatusCode.SeeOther)
        {
            TestContext.Progress.WriteLine(
                $"Login yanit kodu beklendik degil: {loginPost.StatusCode}");
        }
    }

    /// <summary>
    /// HTML'den AntiForgery token degerini parse eder.
    /// Bulunamazsa null doner (token zorunlu olmayan uygulamalar icin toleransli).
    /// </summary>
    private static string? ParseAntiForgeryToken(string html)
    {
        // <input name="__RequestVerificationToken" type="hidden" value="..." />
        var match = Regex.Match(
            html,
            @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : null;
    }
}
