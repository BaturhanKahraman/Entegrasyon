using System.Net;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
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
/// Ürün 360° sayfasının GERÇEK HTTP render yolu — Razor partial'larının doğru DTO/VM'lere
/// bağlandığını ve controller endpoint'lerinin beklenen HTML iskeletini ürettiğini doğrular.
/// Backend veri katmanı (ProductActivityPageManager) ayrı test edilir; burada VIEW WIRING test edilir:
/// sekme iskeleti, pazaryeri kartları (dolu + e-ticaret-kapalı boş durum), sipariş, stok, aktivite.
///
/// Feature-gating: <see cref="StubFeatureService"/> ile e-ticaret açık/kapalı deterministik kontrol edilir.
/// NOT: Testcontainers/PostgreSQL gerektirir (Docker çalışır olmalı).
/// </summary>
[Trait("Category", "Integration")]
public abstract class Product360RenderHttpTestsBase : IntegrationTestBase
{
    protected Guid ProductId;
    protected Guid VariantId;
    protected abstract bool EcommerceEnabled { get; }

    protected Product360RenderHttpTestsBase(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new Product360WebAppFactory(connectionString, EcommerceEnabled);

    protected override async Task OnInitializeAsync()
    {
        await SeedBasicEntitiesAsync();

        using var db = CreateDbContext();
        if (!await db.MarketPlaces.AnyAsync(m => m.Id == 1))
            db.MarketPlaces.Add(new MarketPlace { Id = 1, Name = "Trendyol", CreatedAt = DateTimeOffset.UtcNow });

        var categoryId = (await db.Categories.FirstAsync()).Id;

        ProductId = Guid.NewGuid();
        VariantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = ProductId,
            Title = "360 Render Ürün",
            StockCode = "P360R-001",
            Description = "açıklama",
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow,
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = VariantId,
                    Barcode = "P360RV1",
                    Name = "Krem / 6-9 Ay",
                    SalePrice = 1m, CostPrice = 1m, ListPrice = 1m, ECommercePrice = 1m,
                    CurrencyType = "TRY",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        });
        await db.SaveChangesAsync();
    }
}

/// <summary>E-ticaret AÇIK senaryosu — pazaryeri kartları + aktivite timeline dolu render edilir.</summary>
[Trait("Category", "Integration")]
public sealed class Product360RenderEnabledTests : Product360RenderHttpTestsBase
{
    protected override bool EcommerceEnabled => true;

    public Product360RenderEnabledTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task Detail_page_renders_360_tab_skeleton()
    {
        var client = CreateClient();

        var html = await client.GetStringAsync($"/products/{ProductId}");

        html.Should().Contain("Aktivite");
        html.Should().Contain("Siparişler");
        html.Should().Contain("Stok Hareketleri");
        // Pazaryeri kartları lazy-load container'ı detay sayfasında olmalı
        html.Should().Contain($"/products/{ProductId}/marketplace-cards");
        // Sekmeler lazy-load endpoint'lerini hedeflemeli
        html.Should().Contain($"/products/{ProductId}/orders");
        html.Should().Contain($"/products/{ProductId}/stock-movements");
    }

    [Fact]
    public async Task MarketplaceCards_with_data_renders_card_with_status()
    {
        using (var db = CreateDbContext())
        {
            db.ProductMarketplaces.Add(new ProductMarketplace
            {
                ProductId = ProductId,
                MarketPlaceId = 1,
                Status = MarketplaceProductStatus.Published,
                IsApproved = true,
                ExternalProductId = "TY-CONTENT-42",
                LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-2)
            });
            await db.SaveChangesAsync();
        }
        var client = CreateClient();

        var html = await client.GetStringAsync($"/products/{ProductId}/marketplace-cards");

        html.Should().Contain("Trendyol");
        html.Should().Contain("Pazaryeri Durumu");
        html.Should().Contain("mp-card");
    }

    [Fact]
    public async Task Activity_with_logs_renders_timeline()
    {
        using (var db = CreateDbContext())
        {
            db.ProductActivityLogs.Add(new ProductActivityLog
            {
                ProductId = ProductId,
                Message = "Trendyol'a gönderildi",
                ActivityType = ProductActivityType.PublishSent,
                Status = ProductActivityStatus.Info,
                MarketplaceName = "Trendyol",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            });
            await db.SaveChangesAsync();
        }
        var client = CreateClient();

        var html = await client.GetStringAsync($"/products/{ProductId}/activity");

        html.Should().Contain("timeline");
        html.Should().Contain("Trendyol&#x27;a gönderildi");
    }

    [Fact]
    public async Task Orders_with_data_renders_table()
    {
        using (var db = CreateDbContext())
        {
            var orderId = Guid.NewGuid();
            db.Orders.Add(new Order
            {
                Id = orderId,
                OrderNumber = "TY-987654321",
                OrderDate = DateTimeOffset.UtcNow.AddMinutes(-10),
                MarketPlaceId = 1,
                BillingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "x" },
                ShippingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "y" }
            });
            db.OrderItems.Add(new OrderItem
            {
                OrderId = orderId, ProductId = VariantId, Quantity = 2, UnitPrice = 10m,
                ProductColor = "Krem", ProductSize = "6-9 Ay"
            });
            await db.SaveChangesAsync();
        }
        var client = CreateClient();

        var html = await client.GetStringAsync($"/products/{ProductId}/orders");

        html.Should().Contain("TY-987654321");
        html.Should().Contain("Krem / 6-9 Ay");
        html.Should().Contain("Trendyol");
    }

    [Fact]
    public async Task StockMovements_with_data_renders_grouped_by_variant()
    {
        using (var db = CreateDbContext())
        {
            db.StockMovements.AddRange(
                new StockMovement { BranchOfficeId = 1, ProductVariantId = VariantId, Type = StockMovementType.InitialStock, Quantity = 10, StockBefore = 0, StockAfter = 10, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10) },
                new StockMovement { BranchOfficeId = 1, ProductVariantId = VariantId, Type = StockMovementType.Sale, Quantity = -2, StockBefore = 10, StockAfter = 8, ReferenceId = "POS-1", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
            await db.SaveChangesAsync();
        }
        var client = CreateClient();

        var html = await client.GetStringAsync($"/products/{ProductId}/stock-movements");

        html.Should().Contain("Krem / 6-9 Ay");
        html.Should().Contain("İlk Stok");
        html.Should().Contain("Satış");
        // Variant başlığında güncel stok (en yeni hareketin StockAfter'ı = 8)
        html.Should().Contain("Mevcut: 8");
    }
}

/// <summary>E-ticaret KAPALI senaryosu — zarif pasif/boş durumlar render edilir (KK gating view tarafı).</summary>
[Trait("Category", "Integration")]
public sealed class Product360RenderDisabledTests : Product360RenderHttpTestsBase
{
    protected override bool EcommerceEnabled => false;

    public Product360RenderDisabledTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task MarketplaceCards_disabled_renders_empty_state()
    {
        var client = CreateClient();

        var html = await client.GetStringAsync($"/products/{ProductId}/marketplace-cards");

        html.Should().Contain("empty");
        html.Should().Contain("Pazaryerine Gönder");
        html.Should().NotContain("mp-card");
    }

    [Fact]
    public async Task Activity_disabled_renders_empty_state()
    {
        var client = CreateClient();

        var html = await client.GetStringAsync($"/products/{ProductId}/activity");

        html.Should().Contain("empty");
        html.Should().NotContain("<ul class=\"timeline\">");
    }

    [Fact]
    public async Task Orders_and_stock_work_without_ecommerce()
    {
        // Fiziksel mağaza siparişi + stok — feature-gate YOK, e-ticaret kapalıyken de çalışmalı
        using (var db = CreateDbContext())
        {
            var orderId = Guid.NewGuid();
            db.Orders.Add(new Order
            {
                Id = orderId, OrderNumber = "POS-00045", OrderDate = DateTimeOffset.UtcNow,
                MarketPlaceId = null,
                BillingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "x" },
                ShippingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "y" }
            });
            db.OrderItems.Add(new OrderItem { OrderId = orderId, ProductId = VariantId, Quantity = 1, UnitPrice = 5m });
            db.StockMovements.Add(new StockMovement { BranchOfficeId = 1, ProductVariantId = VariantId, Type = StockMovementType.Sale, Quantity = -1, StockBefore = 5, StockAfter = 4, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        var client = CreateClient();

        var ordersHtml = await client.GetStringAsync($"/products/{ProductId}/orders");
        var stockHtml = await client.GetStringAsync($"/products/{ProductId}/stock-movements");

        ordersHtml.Should().Contain("POS-00045");
        ordersHtml.Should().Contain("Mağaza");
        stockHtml.Should().Contain("Krem / 6-9 Ay");
    }
}

/// <summary>
/// 360° render testleri için kimlik dogrulamali + feature-gating stub'lı factory.
/// TestAuthHandler → [Authorize] gecer. StubFeatureService → e-ticaret açık/kapalı kontrolu.
/// </summary>
file sealed class Product360WebAppFactory : IntegrationTestWebAppFactory
{
    private readonly string _connectionString;
    private readonly bool _ecommerceEnabled;

    public Product360WebAppFactory(string connectionString, bool ecommerceEnabled) : base(connectionString)
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
                new Product360StubTenantRegistry(_connectionString));

            services.RemoveAll<IFeatureService>();
            services.AddScoped<IFeatureService>(_ => new StubFeatureService(_ecommerceEnabled));

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

file sealed class StubFeatureService(bool enabled) : IFeatureService
{
    public Task<bool> IsFeatureEnabledAsync(string permissionKey) => Task.FromResult(enabled);
    public Task<IReadOnlySet<string>> GetEnabledFeaturesAsync() =>
        Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
}

file sealed class Product360StubTenantRegistry(string connectionString) : ITenantRegistryDataSource
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
