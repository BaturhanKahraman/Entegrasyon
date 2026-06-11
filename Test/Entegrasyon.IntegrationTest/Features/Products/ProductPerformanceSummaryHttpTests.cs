using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Entegrasyon.IntegrationTest.Features.Products;

/// <summary>
/// GET /products/{id}/performance-summary lazy-load partial'ının GERÇEK HTTP render yolu (PA #76).
/// Aggregate hesabı ProductPerformanceManager'da ayrı test edilir; burada CONTROLLER WIRING +
/// view render edilir: satışlı/boş durum + daysPast querystring penceresi.
///
/// Dayanıklı kontrat: kök marker `id="product-performance"` (Designer redesign'da KORUMALI) +
/// veri değerleri (pazaryeri adı, net satış adedi). Designer KPI kartlarını cilalar.
/// NOT: Testcontainers/PostgreSQL gerektirir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProductPerformanceSummaryHttpTests : IntegrationTestBase
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Guid _productId;
    private Guid _variantId;

    public ProductPerformanceSummaryHttpTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new PerfSummaryWebAppFactory(connectionString);

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync();

        using var db = CreateDbContext();
        if (!await db.Users.AnyAsync(u => u.Id == TestUserId))
        {
            db.Users.Add(new Entegrasyon.Entity.User.ApplicationUser
            {
                Id = TestUserId, Name = "Integration", Surname = "Admin", FullName = "Integration Admin",
                Email = "perf-admin@test.local", UserName = "perf-admin",
                NormalizedUserName = "PERF-ADMIN", NormalizedEmail = "PERF-ADMIN@TEST.LOCAL",
                IsActive = true, CreatedAt = DateTimeOffset.UtcNow
            });
        }
        if (!await db.MarketPlaces.AnyAsync(m => m.Id == 1))
            db.MarketPlaces.Add(new MarketPlace { Id = 1, Name = "Trendyol", CreatedAt = DateTimeOffset.UtcNow });

        var categoryId = (await db.Categories.FirstAsync()).Id;
        _productId = Guid.NewGuid();
        _variantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = _productId, Title = "Perf Ürün", StockCode = "PERF-001", Description = "x",
            CategoryId = categoryId, CreatedAt = DateTimeOffset.UtcNow,
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = _variantId, Barcode = "PERFV1", Name = "Krem / 6-9 Ay",
                    SalePrice = 50m, CostPrice = 20m, ListPrice = 60m, ECommercePrice = 50m,
                    CurrencyType = "TRY", CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedOrderAsync(int quantity, decimal unitPrice, int daysAgo)
    {
        using var db = CreateDbContext();
        var orderId = Guid.NewGuid();
        db.Orders.Add(new Order
        {
            Id = orderId, OrderNumber = $"TY-{daysAgo}-{quantity}",
            OrderDate = DateTimeOffset.UtcNow.AddDays(-daysAgo), MarketPlaceId = 1,
            BillingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "x" },
            ShippingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "y" }
        });
        db.OrderItems.Add(new OrderItem
        {
            OrderId = orderId, ProductId = _variantId, Quantity = quantity, UnitPrice = unitPrice,
            ProductColor = "Krem", ProductSize = "6-9 Ay"
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Performance_summary_with_sales_renders_marker_and_data()
    {
        await SeedOrderAsync(quantity: 3, unitPrice: 50m, daysAgo: 5);
        var client = CreateClient();

        var html = System.Net.WebUtility.HtmlDecode(
            await client.GetStringAsync($"/products/{_productId}/performance-summary"));

        html.Should().Contain("id=\"product-performance\"");   // dayanıklı kök marker
        html.Should().Contain("Trendyol");                      // marketplace breakdown
        html.Should().Contain("3");                             // net satış adedi
    }

    [Fact]
    public async Task Performance_summary_without_sales_renders_zero_state_not_500()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/products/{_productId}/performance-summary");
        var html = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue("satış olmasa da 200 dönmeli (null/500 değil)");
        html.Should().Contain("id=\"product-performance\"");
    }

    [Fact]
    public async Task Performance_summary_respects_dayspast_window()
    {
        // Sipariş 20 gün önce; daysPast=7 ile pencere DIŞINDA → 0 satış.
        await SeedOrderAsync(quantity: 9, unitPrice: 50m, daysAgo: 20);
        var client = CreateClient();

        var html = System.Net.WebUtility.HtmlDecode(
            await client.GetStringAsync($"/products/{_productId}/performance-summary?daysPast=7"));

        html.Should().Contain("id=\"product-performance\"");
        html.Should().NotContain("Trendyol");   // pencere dışı → marketplace breakdown boş
    }
}

/// <summary>Performans özeti HTTP testi için kimlik doğrulamalı factory.</summary>
file sealed class PerfSummaryWebAppFactory : IntegrationTestWebAppFactory
{
    private readonly string _connectionString;

    public PerfSummaryWebAppFactory(string connectionString) : base(connectionString)
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
                new PerfSummaryStubTenantRegistry(_connectionString));

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

file sealed class PerfSummaryStubTenantRegistry(string connectionString) : ITenantRegistryDataSource
{
    public Task<IReadOnlyList<TenantRegistryEntry>> GetAllTenantsAsync()
        => Task.FromResult<IReadOnlyList<TenantRegistryEntry>>(
        [
            new TenantRegistryEntry(
                TenantId: 1, Subdomain: "dev", CompanyName: "Development",
                ConnectionString: connectionString, IsActive: true, LicenseType: "Enterprise")
        ]);
}
