using System.Net;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Entegrasyon.Business.Tenants;
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

namespace Entegrasyon.IntegrationTest.Features.BulkOperations;

/// <summary>
/// Export'un GERCEK HTTP yolu icin uctan uca regresyon testleri:
/// antiforgery (AutoValidateAntiforgeryToken) + model binding (dateFrom/dateTo)
/// + BulkOperationManager export zinciri. Unit testler controller'i dogrudan
/// cagirdigindan antiforgery filtresini bypass eder; bu testler o bosluğu kapatir
/// (export-with-columns POST'u token olmadan 400 vermeli; token ile 200 + xlsx;
/// tarih araligi sonucu daraltmali).
///
/// NOT: Testcontainers/PostgreSQL gerektirir (Docker calisir olmali). Docker kapaliyken
/// derlenir ama kosmaz — QA fazinda kosulur.
/// </summary>
[Trait("Category", "Integration")]
public class BulkExportHttpTests : IntegrationTestBase
{
    private const int StockExportType = 6; // BulkOperationType.StockExport

    public BulkExportHttpTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new AuthenticatedExportWebAppFactory(connectionString);

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync(brandName: "Export Marka", categoryName: "Export Kategori");
    }

    [Fact]
    public async Task ExportWithColumns_WithoutAntiforgeryToken_Returns400()
    {
        // Arrange — taze client (GET yapilmadi → ne cookie ne token var)
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["type"] = StockExportType.ToString()
        });

        // Act
        var response = await client.PostAsync("/bulk-operations/export-with-columns", form);

        // Assert — global AutoValidateAntiforgeryToken token olmadan 400 dondurur.
        // (Bu, unit testlerin bypass ettigi ve canliyi kiran orijinal bug'in regresyon kilidi.)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportWithColumns_WithValidToken_Returns200Xlsx()
    {
        // Arrange
        await SeedProductWithStockAsync("EXP-001", stock: 5);
        var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client);

        // Act
        var response = await client.PostAsync("/bulk-operations/export-with-columns",
            BuildForm(token, dateFrom: null, dateTo: null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
        CountDataRows(bytes).Should().Be(1, "tek stok kaydi seed edildi");
    }

    [Fact]
    public async Task ExportWithColumns_WithDateRange_NarrowsRows()
    {
        // Arrange — biri eski (2000), biri yeni (now) iki stok kaydi
        var (oldProductId, _) = await SeedProductWithStockAsync("EXP-OLD", stock: 3);
        await SeedProductWithStockAsync("EXP-NEW", stock: 7);
        await BackdateProductCreatedAtAsync(oldProductId, new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var client = CreateClient();

        // Act 1 — tarihsiz: her iki kayit
        var allToken = await GetAntiforgeryTokenAsync(client);
        var allResponse = await client.PostAsync("/bulk-operations/export-with-columns",
            BuildForm(allToken, dateFrom: null, dateTo: null));

        // Act 2 — dateFrom = 2010: sadece yeni kayit (eski 2000 < 2010 → haric)
        var filteredToken = await GetAntiforgeryTokenAsync(client);
        var filteredResponse = await client.PostAsync("/bulk-operations/export-with-columns",
            BuildForm(filteredToken,
                dateFrom: new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero),
                dateTo: null));

        // Assert
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var allRows = CountDataRows(await allResponse.Content.ReadAsByteArrayAsync());
        var filteredRows = CountDataRows(await filteredResponse.Content.ReadAsByteArrayAsync());

        allRows.Should().Be(2);
        filteredRows.Should().Be(1, "dateFrom=2010 eski (2000) kaydi dislamali");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// export-columns partial'ini GET edip (antiforgery cookie'sini set eder)
    /// hidden __RequestVerificationToken degerini cikarir.
    /// </summary>
    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync($"/bulk-operations/export-columns?type={StockExportType}");
        var match = Regex.Match(html, "__RequestVerificationToken.*?value=\"([^\"]+)\"", RegexOptions.Singleline);
        match.Success.Should().BeTrue("export-columns partial'i antiforgery token icermeli");
        return match.Groups[1].Value;
    }

    private static FormUrlEncodedContent BuildForm(string token, DateTimeOffset? dateFrom, DateTimeOffset? dateTo)
    {
        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["type"] = StockExportType.ToString()
        };
        if (dateFrom.HasValue)
            fields["dateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd");
        if (dateTo.HasValue)
            fields["dateTo"] = dateTo.Value.ToString("yyyy-MM-dd");

        return new FormUrlEncodedContent(fields);
    }

    private async Task BackdateProductCreatedAtAsync(Guid productId, DateTimeOffset createdAt)
    {
        // ExecuteUpdateAsync, SaveChanges audit interceptor'ini bypass eder
        // (interceptor Added entity'lerde CreatedAt'i her zaman UtcNow yapar).
        using var db = CreateDbContext();
        await db.MainProducts
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.CreatedAt, createdAt));
    }

    private static int CountDataRows(byte[] xlsx)
    {
        using var stream = new MemoryStream(xlsx);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var lastRow = sheet.LastRowUsed();
        return lastRow is null ? 0 : lastRow.RowNumber() - 1; // baslik satirini dus
    }
}

/// <summary>
/// HTTP-seviye export testi icin kimlik dogrulamali factory:
/// - TestAuthHandler default scheme → [Authorize] gecer (login akisi bypass).
/// - Antiforgery KAPATILMAZ (test edilen sey bu).
/// - Tenant resolution: env "Testing" + stub ITenantRegistryDataSource → AdminPanel DB
///   bagimliligi yok; "dev" tenant test container CS'ine isaret eder.
/// </summary>
file sealed class AuthenticatedExportWebAppFactory : IntegrationTestWebAppFactory
{
    private readonly string _connectionString;

    public AuthenticatedExportWebAppFactory(string connectionString) : base(connectionString)
        => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Tenant:DefaultSubdomain = "dev" → TenantResolutionMiddleware'in
        // localhost'ta "dev" tenant'ini fallback olarak secmesini saglar.
        // NOT: UseEnvironment("Testing") bilerek KULLANILMIYOR — ApplicationLifetimeManager
        // "Testing" ortaminda MigrateDatabase() cagirir; bu da IntegrationTestBase.InitializeAsync()
        // ile eszamanli migration race condition'ina yol acar (42P01: table "InboxState" does not exist).
        // "IntegrationTest" ortami (base factory tarafindan atandi) migration'i initialize asamasina birakir.
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tenant:DefaultSubdomain"] = "dev"
            }));

        builder.ConfigureTestServices(services =>
        {
            // Tenant kaydini stub'la — AdminPanel DB'ye gitmeden "dev" tenant'i coz.
            services.RemoveAll<ITenantRegistryDataSource>();
            services.AddSingleton<ITenantRegistryDataSource>(
                new StubTenantRegistryDataSource(_connectionString));

            // Sabit kimlikli auth — antiforgery'yi bozmadan login akisini bypass et.
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
