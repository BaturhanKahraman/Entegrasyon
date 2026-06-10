using System.Net;
using System.Text.RegularExpressions;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// 320e33e4 (SecurityStamp auto-logout) regresyonunun uctan uca kilidi.
///
/// Bug: migration SecurityStamp'i NULLABLE ekledi → mevcut kullanici satirlari NULL stamp.
/// SecurityStampCookieEvents.ValidatePrincipal fail-closed olduğundan, login basarili olsa da
/// (cookie'ye yeni stamp claim'i yazilir) DB'de stamp NULL kaldigi icin SONRAKI authenticated
/// istek reddedilir → sonsuz logout loop, app kullanilamaz.
///
/// Kok neden: AuthService.LoginAsync null stamp'i set ediyordu AMA global no-tracking altinda
/// context.Update(user) cagrilmadigi icin SaveChanges no-op oluyordu (0ad50995 footgun) →
/// stamp DB'ye PERSIST olmuyor, cookie'deki yeni stamp ile DB'deki NULL eslesmiyor.
///
/// Bu test GERCEK cookie-auth pipeline'i (SecurityStampCookieEvents dahil) uzerinde calisir —
/// TestAuthHandler ile BYPASS ETMEZ. NULL stamp'li kullanici login olur → /profile 200 donmeli
/// (login'e redirect ETMEMELI). Ayrica login sonrasi DB'de stamp non-null + cookie ile eslesmis olmali.
/// </summary>
[Trait("Category", "Integration")]
public class SecurityStampLoginLoopRegressionTests : IntegrationTestBase
{
    private const string Password = "123456789";

    public SecurityStampLoginLoopRegressionTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new RealCookieAuthWebAppFactory(connectionString);

    /// <summary>
    /// Login akisi sonunda ActiveBranchOffice cozumlemesi yapar; HQ fallback'i icin sistemde
    /// IsHeadquarters=true bir sube ofisi bulunmali (gercek app'te seed ile gelir). Yoksa login
    /// 500 atar (oturum dogrulama testiyle alakasiz). Idempotent seed.
    /// </summary>
    protected override async Task OnInitializeAsync()
    {
        using var db = CreateDbContext();
        if (!await db.BranchOffices.AnyAsync(b => b.IsHeadquarters))
        {
            db.BranchOffices.Add(new Entegrasyon.Entity.BranchOffice
            {
                Name = "Genel Merkez",
                IsHeadquarters = true,
                IsDefaultMarketPlaceStock = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>NULL SecurityStamp'li, bilinen bcrypt parolasiyla aktif bir kullanici seed eder.</summary>
    private async Task<(Guid Id, string UserName)> SeedNullStampUserAsync()
    {
        using var db = CreateDbContext();
        var id = Guid.NewGuid();
        var suffix = id.ToString("N")[..12]; // UserName kolonu varchar(30)
        var userName = $"loop{suffix}";
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            Name = "Loop",
            Surname = "Test",
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            IsActive = true,
            NeedsTakeNewPassword = false,
            PasswordHashVersion = 1,
            BcryptPasswordHash = HashingHelper.CreateBcryptHash(Password),
            SecurityStamp = null, // ← regresyonun cekirdegi: NULL stamp
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return (id, userName);
    }

    [Fact]
    public async Task NullStampUser_LogsIn_ThenAuthenticatedRequest_Returns200_NotLoginRedirect()
    {
        // Arrange — NULL stamp'li kullanici
        var (userId, userName) = await SeedNullStampUserAsync();
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act 1 — login (gercek form POST, antiforgery dahil)
        var loginResponse = await PostLoginAsync(client, userName, Password);

        // Login basarili → ana sayfaya 302 redirect (login formuna degil)
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        loginResponse.Headers.Location!.OriginalString.Should().NotContain("/auth/login",
            "basarili login ana sayfaya yonlendirmeli, login formuna degil");

        // Act 2 — login sonrasi authenticated istek (cookie tasinir)
        var profileResponse = await client.GetAsync("/profile");

        // Assert — REGRESYON KILIDI: NULL stamp logout loop'a dusurmemeli.
        // Bug aktifken: 302 → /auth/login?ReturnUrl=... (validator reddediyor).
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "NULL stamp'li kullanici login backfill ile gecerli stamp almali; authenticated istek 200 donmeli");

        // Stamp DB'ye persist oldu mu? (kok neden: Update eksikti → no-op)
        using var db = CreateDbContext();
        var dbStamp = await db.Users.Where(u => u.Id == userId).Select(u => u.SecurityStamp).FirstAsync();
        dbStamp.Should().NotBeNullOrEmpty("login backfill stamp'i DB'ye persist etmeli (AsTracking/Update)");
    }

    [Fact]
    public async Task AfterLogin_DeactivatingUser_DropsExistingSession()
    {
        // 320e33e4'un asil ozelligi backfill sonrasi da calismali:
        // backfill ile gecerli stamp alan kullanicinin oturumu, pasiflestirilince DUSER.
        var (userId, userName) = await SeedNullStampUserAsync();
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        (await PostLoginAsync(client, userName, Password)).StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await client.GetAsync("/profile")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Kullaniciyi pasiflestir (stamp bump + IsActive=false)
        using (var db = CreateDbContext())
        {
            await db.Users.Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.IsActive, false)
                    .SetProperty(u => u.SecurityStamp, Guid.NewGuid().ToString("N")));
        }

        // Sonraki istek: oturum dusmeli → login'e redirect
        var afterDeactivate = await client.GetAsync("/profile");
        afterDeactivate.StatusCode.Should().Be(HttpStatusCode.Redirect);
        afterDeactivate.Headers.Location!.OriginalString.Should().Contain("/auth/login",
            "pasiflestirilen/stamp'i degisen kullanicinin oturumu dusmeli (320e33e4 ozelligi korunur)");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string userName, string password)
    {
        // GET login → antiforgery cookie + hidden token
        var loginHtml = await client.GetStringAsync("/auth/login");
        var token = ExtractAntiforgeryToken(loginHtml);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Username"] = userName,
            ["Password"] = password,
            ["RememberMe"] = "false"
        });
        return await client.PostAsync("/auth/login", form);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html, "__RequestVerificationToken.*?value=\"([^\"]+)\"", RegexOptions.Singleline);
        match.Success.Should().BeTrue("login formu antiforgery token icermeli");
        return match.Groups[1].Value;
    }
}

/// <summary>
/// GERCEK cookie-auth + SecurityStampCookieEvents pipeline'ini koruyan factory.
/// TestAuthHandler EKLENMEZ — login akisi ve oturum dogrulama gercek kod yoluyla calisir.
/// Sadece tenant kaydi stub'lanir (AdminPanel DB bagimliligi yok).
/// </summary>
file sealed class RealCookieAuthWebAppFactory : IntegrationTestWebAppFactory
{
    private readonly string _connectionString;

    public RealCookieAuthWebAppFactory(string connectionString) : base(connectionString)
        => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tenant:DefaultSubdomain"] = "dev"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITenantRegistryDataSource>();
            services.AddSingleton<ITenantRegistryDataSource>(
                new StubTenantRegistryDataSource(_connectionString));
        });
    }
}

/// <summary>Tek "dev" tenant donen stub — test container connection string'ine isaret eder.</summary>
file sealed class StubTenantRegistryDataSource(string connectionString) : ITenantRegistryDataSource
{
    public Task<IReadOnlyList<TenantRegistryEntry>> GetAllTenantsAsync()
        => Task.FromResult<IReadOnlyList<TenantRegistryEntry>>(
        [
            new TenantRegistryEntry(
                TenantId: 1,
                Subdomain: "dev",
                CompanyName: "Development",
                ConnectionString: connectionString,
                IsActive: true,
                LicenseType: "Enterprise")
        ]);
}
