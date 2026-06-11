using System.Net;
using System.Text.RegularExpressions;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Products;
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

namespace Entegrasyon.IntegrationTest.Features.Products;

/// <summary>
/// /products/{id}/storefront sayfasının GERÇEK HTTP yolu (PA #75 — storefront ayrımı).
/// Eski ürün-detay-içi lazy-load store-settings partial'ı ayrı tam-sayfaya taşındı:
///   - GET  /products/{id}/storefront         → tam sayfa (SEO + fiyat + yayın)
///   - POST /products/{id}/storefront         → kaydet (PRG redirect)
///   - POST /products/{id}/storefront/publish → kaydet + yayınla (PRG redirect)
///   - GET  /products/{id}/store-settings     → 301 → /storefront (geri uyumluluk)
///
/// Feature-gating: <see cref="StorefrontStubFeatureService"/> ile e-ticaret açık/kapalı deterministik.
/// E-ticaret kapalıyken "Yayınla" engellenir (sessizce başarı simüle EDİLMEZ).
/// NOT: Testcontainers/PostgreSQL gerektirir (Docker çalışır olmalı).
/// </summary>
[Trait("Category", "Integration")]
public abstract class StorefrontSettingsHttpTestsBase : IntegrationTestBase
{
    // TestAuthHandler sabit kullanıcı id'si — AddLog'un ApplicationUserId FK'si için Users'ta olmalı.
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    protected Guid ProductId;
    protected Guid VariantId;
    protected abstract bool EcommerceEnabled { get; }

    protected StorefrontSettingsHttpTestsBase(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new StorefrontWebAppFactory(connectionString, EcommerceEnabled);

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync();

        using var db = CreateDbContext();
        if (!await db.Users.AnyAsync(u => u.Id == TestUserId))
        {
            db.Users.Add(new Entegrasyon.Entity.User.ApplicationUser
            {
                Id = TestUserId,
                Name = "Integration", Surname = "Admin", FullName = "Integration Admin",
                Email = "integration-admin@test.local", UserName = "int-admin",
                NormalizedUserName = "INT-ADMIN", NormalizedEmail = "INTEGRATION-ADMIN@TEST.LOCAL",
                IsActive = true, CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var categoryId = (await db.Categories.FirstAsync()).Id;
        ProductId = Guid.NewGuid();
        VariantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = ProductId,
            Title = "Storefront Test Ürün",
            StockCode = "SF-001",
            Description = "açıklama",
            CategoryId = categoryId,
            IsPublished = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = VariantId, Barcode = "SFV1", Name = "Krem / 6-9 Ay",
                    SalePrice = 100m, CostPrice = 50m, ListPrice = 120m, ECommercePrice = 0m,
                    CurrencyType = "TRY", CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });
        await db.SaveChangesAsync();
    }

    protected async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync($"/products/{ProductId}/storefront");
        var match = Regex.Match(html, "__RequestVerificationToken.*?value=\"([^\"]+)\"", RegexOptions.Singleline);
        match.Success.Should().BeTrue("storefront sayfası antiforgery token içermeli");
        return match.Groups[1].Value;
    }
}

/// <summary>E-ticaret AÇIK — tam fonksiyonel storefront sayfası.</summary>
[Trait("Category", "Integration")]
public sealed class StorefrontSettingsEnabledTests : StorefrontSettingsHttpTestsBase
{
    protected override bool EcommerceEnabled => true;

    public StorefrontSettingsEnabledTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task Storefront_page_renders_seo_price_and_publish()
    {
        var client = CreateClient();

        var html = System.Net.WebUtility.HtmlDecode(
            await client.GetStringAsync($"/products/{ProductId}/storefront"));

        html.Should().Contain("Storefront Ayarları");        // başlık/breadcrumb
        html.Should().Contain("name=\"SeoSlug\"");
        html.Should().Contain("name=\"SeoTitle\"");
        html.Should().Contain("name=\"VariantPrices[0].ECommercePrice\"");
        html.Should().Contain("__RequestVerificationToken");
    }

    [Fact]
    public async Task Detail_page_no_longer_lazy_loads_store_settings()
    {
        var client = CreateClient();

        var html = await client.GetStringAsync($"/products/{ProductId}");

        html.Should().NotContain($"/products/{ProductId}/store-settings");
        // Storefront ayarlarına bir link/buton ile gidilebilmeli (kabul kriteri #3)
        html.Should().Contain($"/products/{ProductId}/storefront");
    }

    [Fact]
    public async Task Old_store_settings_route_redirects_to_storefront()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/products/{ProductId}/store-settings");

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().EndWith($"/products/{ProductId}/storefront");
    }

    [Fact]
    public async Task Save_persists_seo_and_redirects()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["SeoTitle"] = "SEO Başlık",
            ["SeoSlug"] = "storefront-test-slug",
            ["SeoDescription"] = "açıklama",
            ["SeoKeywords"] = "a, b",
            ["VariantPrices[0].VariantId"] = VariantId.ToString(),
            ["VariantPrices[0].VariantName"] = "Krem / 6-9 Ay",
            ["VariantPrices[0].SalePrice"] = "100",
            ["VariantPrices[0].ECommercePrice"] = "89,90"
        });

        var response = await client.PostAsync($"/products/{ProductId}/storefront", form);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().EndWith($"/products/{ProductId}/storefront");

        using var db = CreateDbContext();
        var product = await db.MainProducts.AsNoTracking().FirstAsync(p => p.Id == ProductId);
        product.SeoSlug.Should().Be("storefront-test-slug");
        product.SeoTitle.Should().Be("SEO Başlık");
    }

    [Fact]
    public async Task Publish_sets_is_published_and_redirects()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["SeoSlug"] = "publish-slug",
            ["VariantPrices[0].VariantId"] = VariantId.ToString(),
            ["VariantPrices[0].VariantName"] = "Krem / 6-9 Ay",
            ["VariantPrices[0].SalePrice"] = "100",
            ["VariantPrices[0].ECommercePrice"] = "100"
        });

        var response = await client.PostAsync($"/products/{ProductId}/storefront/publish", form);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var db = CreateDbContext();
        var product = await db.MainProducts.AsNoTracking().FirstAsync(p => p.Id == ProductId);
        product.IsPublished.Should().BeTrue();
    }
}

/// <summary>E-ticaret KAPALI — sayfa açılır ama "Yayınla" engellenir (sessiz başarı YOK).</summary>
[Trait("Category", "Integration")]
public sealed class StorefrontSettingsDisabledTests : StorefrontSettingsHttpTestsBase
{
    protected override bool EcommerceEnabled => false;

    public StorefrontSettingsDisabledTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task Page_warns_when_ecommerce_off()
    {
        var client = CreateClient();

        var html = System.Net.WebUtility.HtmlDecode(
            await client.GetStringAsync($"/products/{ProductId}/storefront"));

        html.Should().Contain("E-ticaret");   // uyarı bandı (sabit metin: "E-ticaret modülü...")
    }

    [Fact]
    public async Task Publish_is_blocked_when_ecommerce_off()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["SeoSlug"] = "blocked-slug",
            ["VariantPrices[0].VariantId"] = VariantId.ToString(),
            ["VariantPrices[0].VariantName"] = "Krem / 6-9 Ay",
            ["VariantPrices[0].SalePrice"] = "100",
            ["VariantPrices[0].ECommercePrice"] = "100"
        });

        await client.PostAsync($"/products/{ProductId}/storefront/publish", form);

        // Yayınla engellenir: redirect olsa bile ürün YAYINLANMAMALI.
        using var db = CreateDbContext();
        var product = await db.MainProducts.AsNoTracking().FirstAsync(p => p.Id == ProductId);
        product.IsPublished.Should().BeFalse("e-ticaret kapalıyken yayınlama sessizce simüle edilmemeli");
    }
}

/// <summary>Storefront HTTP testleri için kimlik doğrulamalı + feature-gating stub'lı factory.</summary>
file sealed class StorefrontWebAppFactory : IntegrationTestWebAppFactory
{
    private readonly string _connectionString;
    private readonly bool _ecommerceEnabled;

    public StorefrontWebAppFactory(string connectionString, bool ecommerceEnabled) : base(connectionString)
    {
        _connectionString = connectionString;
        _ecommerceEnabled = ecommerceEnabled;
    }

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
                new StorefrontStubTenantRegistry(_connectionString));

            services.RemoveAll<IFeatureService>();
            services.AddScoped<IFeatureService>(_ => new StorefrontStubFeatureService(_ecommerceEnabled));

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

file sealed class StorefrontStubFeatureService(bool enabled) : IFeatureService
{
    public Task<bool> IsFeatureEnabledAsync(string permissionKey) => Task.FromResult(enabled);
    public Task<IReadOnlySet<string>> GetEnabledFeaturesAsync() =>
        Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
}

file sealed class StorefrontStubTenantRegistry(string connectionString) : ITenantRegistryDataSource
{
    public Task<IReadOnlyList<TenantRegistryEntry>> GetAllTenantsAsync()
        => Task.FromResult<IReadOnlyList<TenantRegistryEntry>>(
        [
            new TenantRegistryEntry(
                TenantId: 1, Subdomain: "dev", CompanyName: "Development",
                ConnectionString: connectionString, IsActive: true, LicenseType: "Enterprise")
        ]);
}
