using System.Net;
using System.Text.RegularExpressions;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Help;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Entegrasyon.IntegrationTest.Features.Help;

/// <summary>
/// /help/new (kullanici) + /help (admin) icin uctan uca HTTP testleri:
/// antiforgery + model binding + AutoValidationFilter + view render.
/// Manager testi bu zinciri bypass ettiginden controller/route/view bug'larini
/// ancak gercek HTTP yolu yakalar.
/// </summary>
[Trait("Category", "Integration")]
public class HelpHttpTests : IntegrationTestBase
{
    public HelpHttpTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    protected override IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new AuthenticatedHelpWebAppFactory(connectionString);

    protected override async Task OnInitializeAsync()
    {
        using var db = CreateDbContext();
        if (!await db.Users.AnyAsync(u => u.Id == TestUserId))
        {
            db.Users.Add(new Entegrasyon.Entity.User.ApplicationUser
            {
                Id = TestUserId,
                Name = "Integration",
                Surname = "Admin",
                FullName = "Integration Admin",
                Email = "integration-admin@test.local",
                UserName = "int-admin",
                NormalizedUserName = "INT-ADMIN",
                NormalizedEmail = "INTEGRATION-ADMIN@TEST.LOCAL",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task PostNew_WithoutAntiforgeryToken_DoesNotPersist()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Category"] = "Bug",
            ["Subject"] = "Tokensiz",
            ["Message"] = "Tokensiz mesaj"
        });

        var response = await client.PostAsync("/help/new", form);

        response.StatusCode.Should().NotBe(HttpStatusCode.Redirect);
        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400);

        using var db = CreateDbContext();
        (await db.Set<HelpRequest>().AnyAsync()).Should().BeFalse("token yoksa kayit olusmamali");
    }

    [Fact]
    public async Task PostNew_WithToken_PersistsAndRedirects()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/help/new");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Category"] = "Suggestion",
            ["Subject"] = "Karanlik tema",
            ["Message"] = "Karanlik tema ekleyin lutfen."
        });

        var response = await client.PostAsync("/help/new", form);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Be("/help/new");

        using var db = CreateDbContext();
        var saved = await db.Set<HelpRequest>().AsNoTracking()
            .FirstOrDefaultAsync(h => h.Subject == "Karanlik tema");
        saved.Should().NotBeNull("token ile gonderilen talep kaydedilmeli");
        saved!.UserId.Should().Be(TestUserId);
        saved.Category.Should().Be(HelpRequestCategory.Suggestion);
        saved.Status.Should().Be(HelpRequestStatus.Open);
    }

    [Fact]
    public async Task AdminIndex_RendersSubmittedRequest()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/help/new");
        await client.PostAsync("/help/new", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Category"] = "Bug",
            ["Subject"] = "Admin panelde gorunmeli",
            ["Message"] = "Bu talep admin listesinde cikmali."
        }));

        var html = await client.GetStringAsync("/help");

        html.Should().Contain("Admin panelde gorunmeli");
    }

    [Fact]
    public async Task Resolve_MarksRequestResolved()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var newToken = await GetAntiforgeryTokenAsync(client, "/help/new");
        await client.PostAsync("/help/new", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = newToken,
            ["Category"] = "Question",
            ["Subject"] = "Cozulecek talep",
            ["Message"] = "Lutfen cozun."
        }));

        int id;
        using (var db = CreateDbContext())
            id = await db.Set<HelpRequest>().Select(h => h.Id).FirstAsync();

        // Resolve formunun antiforgery token'i detay sayfasindan alinir.
        var resolveToken = await GetAntiforgeryTokenAsync(client, $"/help/{id}");
        var response = await client.PostAsync($"/help/{id}/resolve", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = resolveToken
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var db2 = CreateDbContext();
        var saved = await db2.Set<HelpRequest>().AsNoTracking().FirstAsync(h => h.Id == id);
        saved.Status.Should().Be(HelpRequestStatus.Resolved);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = Regex.Match(html, "__RequestVerificationToken.*?value=\"([^\"]+)\"", RegexOptions.Singleline);
        match.Success.Should().BeTrue($"{url} antiforgery token icermeli");
        return match.Groups[1].Value;
    }
}

file sealed class AuthenticatedHelpWebAppFactory : IntegrationTestWebAppFactory
{
    private readonly string _connectionString;

    public AuthenticatedHelpWebAppFactory(string connectionString) : base(connectionString)
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
                new StubHelpTenantRegistryDataSource(_connectionString));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            });
        });
    }
}

file sealed class StubHelpTenantRegistryDataSource(string connectionString) : ITenantRegistryDataSource
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
