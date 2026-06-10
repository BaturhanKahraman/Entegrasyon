using System.Net;
using System.Text.RegularExpressions;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Customers;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Entegrasyon.IntegrationTest.Features.Customers;

/// <summary>
/// /customers/create'in GERCEK HTTP yolu icin uctan uca regresyon testleri:
/// antiforgery (AutoValidateAntiforgeryToken) + model binding + AutoValidationFilter.
///
/// Unit/manager testleri controller'i ve filtre zincirini bypass ettiginden bu boslugu
/// yakalayamiyordu. Canli'da gorulen bug: CustomerAddDto'nun non-nullable string alanlari
/// (Name/Surname/NationalIdentity ...) model binder tarafindan "implicitly required"
/// sayiliyordu; KURUMSAL musteri eklerken bu alanlar bos kaldigi icin ModelState gecersiz
/// oluyor, AutoValidationFilter POST'u action'a ulasmadan geri yonlendiriyordu → musteri
/// hic kaydedilmiyordu. Bu test o yolu kilitler.
/// </summary>
[Trait("Category", "Integration")]
public class CustomerCreateHttpTests : IntegrationTestBase
{
    public CustomerCreateHttpTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    // TestAuthHandler'in sabit kullanici id'si — AddLog'un ApplicationUserId FK'si
    // icin Users tablosunda mevcut olmali.
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    protected override IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new AuthenticatedCustomerWebAppFactory(connectionString);

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
    public async Task Create_WithoutAntiforgeryToken_Returns400()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CustomerType"] = "Retail",
            ["Name"] = "Tokensiz"
        });

        var response = await client.PostAsync("/customers/create", form);

        // Antiforgery token olmadan create action'a ULASILMAMALI (global
        // AutoValidateAntiforgeryToken). Ortama gore 400 (BadRequest) ya da
        // 500 (handle edilmemis AntiforgeryValidationException) donebilir; onemli
        // olan basarili PRG redirect'i (302 → /customers) ASLA olmamasi.
        response.StatusCode.Should().NotBe(HttpStatusCode.Redirect);
        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400);
    }

    [Fact]
    public async Task Create_Corporate_WithEmptyRetailFields_PersistsCustomer()
    {
        // Arrange — kurumsal musteri: Ad/Soyad/TC BOS (gercek senaryo)
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["CustomerType"] = "Corporate",
            ["CorporateName"] = "HTTP Test Tekstil A.Ş.",
            ["TaxNumber"] = "1112223334",
            ["Name"] = "",
            ["Surname"] = "",
            ["NationalIdentity"] = "",
            ["PhoneNumber"] = "3121110022",
            ["FullAddress"] = "HTTP Test Adres Ankara"
        });

        // Act
        var response = await client.PostAsync("/customers/create", form);

        // Assert — basariyla action'a ulasip kaydetmeli (PRG: 302 → /customers).
        // Eski non-nullable DTO'da ModelState gecersiz olup 302 → / (Referer yok) donuyordu.
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/customers");

        using var db = CreateDbContext();
        var created = await db.Customers.AsNoTracking()
            .OfType<CorporateCustomer>()
            .FirstOrDefaultAsync(c => c.CorporateName == "HTTP Test Tekstil A.Ş.");

        created.Should().NotBeNull("kurumsal musteri bos ad/soyad ile kaydedilebilmeli");
        created!.TaxNumber.Should().Be("1112223334");
        created.Address.Should().NotBeNull();
        created.Address!.FullAddress.Should().Be("HTTP Test Adres Ankara");
    }

    [Fact]
    public async Task Create_Retail_WithEmptyCorporateFields_PersistsCustomer()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["CustomerType"] = "Retail",
            ["Name"] = "Mehmet",
            ["Surname"] = "Demir",
            ["NationalIdentity"] = "10203040506",
            ["CorporateName"] = "",
            ["TaxNumber"] = "",
            ["PhoneNumber"] = "5331114455",
            ["FullAddress"] = "Bireysel Adres İstanbul"
        });

        var response = await client.PostAsync("/customers/create", form);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/customers");

        using var db = CreateDbContext();
        var created = await db.Customers.AsNoTracking()
            .OfType<RetailCustomer>()
            .FirstOrDefaultAsync(c => c.NationalIdentity == "10203040506");

        created.Should().NotBeNull("bireysel musteri bos firma/vergi alanlari ile kaydedilebilmeli");
        created!.Name.Should().Be("Mehmet");
        created.Address!.FullAddress.Should().Be("Bireysel Adres İstanbul");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/customers/create");
        var match = Regex.Match(html, "__RequestVerificationToken.*?value=\"([^\"]+)\"", RegexOptions.Singleline);
        match.Success.Should().BeTrue("create sayfasi antiforgery token icermeli");
        return match.Groups[1].Value;
    }
}

/// <summary>
/// HTTP-seviye customer create testi icin kimlik dogrulamali factory
/// (BulkExportHttpTests desenini takip eder): TestAuthHandler ile [Authorize] gecer,
/// antiforgery KAPATILMAZ, tenant "dev" → test container DB.
/// </summary>
file sealed class AuthenticatedCustomerWebAppFactory : IntegrationTestWebAppFactory
{
    private readonly string _connectionString;

    public AuthenticatedCustomerWebAppFactory(string connectionString) : base(connectionString)
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
                new StubCustomerTenantRegistryDataSource(_connectionString));

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

file sealed class StubCustomerTenantRegistryDataSource(string connectionString) : ITenantRegistryDataSource
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
