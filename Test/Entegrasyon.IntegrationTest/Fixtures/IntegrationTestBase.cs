using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Collections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;

namespace Entegrasyon.IntegrationTest.Fixtures;

/// <summary>
/// Tum integration testleri icin base class.
/// Her test class'i icin WebApplicationFactory olusturulur, her test sonrasi Respawn ile DB temizlenir.
/// Seed tabloları (Roles, Users, ApplicationSettings vb.) Respawn tarafindan korunur.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlFixture _pgFixture;
    private IntegrationTestWebAppFactory _factory = null!;
    private Respawner _respawner = null!;
    private NpgsqlConnection _dbConnection = null!;

    /// <summary>
    /// WireMock in-process server — marketplace HTTP client'lari icin tek mock kaynak.
    /// Her test class'i ResetAll() cagirip kendi stub'larini kurar. MarketPlace seed'i
    /// SeedMarketPlaceAsync icinde BaseUrl = WireMock.BaseUrl olarak set edilir.
    /// </summary>
    protected WireMockFixture WireMock { get; }

    protected IServiceProvider Services => _factory.Services;
    protected IServiceScope CreateScope() => Services.CreateScope();

    protected IntegrationTestBase(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
    {
        _pgFixture = pgFixture;
        WireMock = wireMock;
    }

    public async Task InitializeAsync()
    {
        // Her test class'i kendi WireMock stub'larini kuracak — onceki class'tan kalan
        // mapping'leri, log entry'leri ve scenario state'lerini temizle.
        WireMock.ResetAll();

        _factory = CreateFactory(_pgFixture.ConnectionString);

        // KRİTİK SIRA — host'u BAŞLATMADAN ÖNCE DB'yi migrate et.
        // _factory.Server / _factory.Services erişimi host'u START eder (EnsureServer); bu da
        // Program.cs'teki ApplicationStarted callback'ini tetikler. O callback
        // AdminPermissionSeeder.EnsureAdminPermissionsAsync()'i KOŞULSUZ çağırıp "Roles" tablosunu
        // sorgular. "IntegrationTest" ortamında app KENDİ migrate ETMEZ (ApplyStartActions yalnızca
        // Development/Testing'de migrate eder) → tablo henüz yok → 42P01 "relation Roles does not exist".
        // Bu, fire-and-forget async callback'te UNOBSERVED exception olarak fırlar ve test host'unu
        // çökertip TÜM run'ı abort eder (55 test). Çözüm: migration'ı host-start'tan ÖNCE, DI/host
        // DIŞINDA bağımsız bir context ile uygula. (DesignTimeDbContextFactory ile aynı desen.)
        await MigrateDatabaseBeforeHostStartAsync(_pgFixture.ConnectionString);

        // Artık host başlatılabilir — ApplicationStarted seeder'ı migrated DB'de sorunsuz çalışır.
        _ = _factory.Server;

        // Setup Respawn — skip seed tables
        _dbConnection = new NpgsqlConnection(_pgFixture.ConnectionString);
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore =
            [
                new Respawn.Graph.Table("__EFMigrationsHistory"),
                new Respawn.Graph.Table("Roles"),
                new Respawn.Graph.Table("Users"),
                new Respawn.Graph.Table("UsersRoles"),
                new Respawn.Graph.Table("ApplicationSettings"),
                new Respawn.Graph.Table("PaymentMethodDefinitions")
            ]
        });

        await OnInitializeAsync();
    }

    /// <summary>
    /// Alt class'lar icin ek initialization hook'u.
    /// </summary>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// DB'yi WebApplicationFactory host'u BAŞLAMADAN önce migrate eder. Host start'ı, boş DB'de
    /// "Roles" sorgulayan ApplicationStarted seeder'ını tetiklediğinden (42P01 → unobserved async →
    /// run abort), migration host-start'tan önce ve DI dışında bağımsız bir context ile yapılır.
    /// Migration assembly'si default olarak IntegrationDbContext'in assembly'sinden (Entegrasyon.DataAccess)
    /// çözülür — DesignTimeDbContextFactory ile birebir aynı yaklaşım.
    /// </summary>
    private static async Task MigrateDatabaseBeforeHostStartAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var dbContext = new IntegrationDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// WebApplicationFactory'yi olusturur. Varsayilan: standart IntegrationTest host.
    /// HTTP-seviye (auth + antiforgery) testi yapan alt class'lar override edip
    /// kimlik dogrulamali bir factory donebilir. Service-seviye testler dokunmaz.
    /// </summary>
    protected virtual IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new(connectionString);

    /// <summary>
    /// Test host'una baglanan bir HttpClient olusturur. Cookie container otomatik
    /// yonetilir (antiforgery cookie'leri istekler arasi tasinir).
    /// </summary>
    protected HttpClient CreateClient(WebApplicationFactoryClientOptions? options = null)
        => _factory.CreateClient(options ?? new WebApplicationFactoryClientOptions());

    public async Task DisposeAsync()
    {
        // Reset DB state for next test class
        if (_dbConnection is { State: System.Data.ConnectionState.Open })
        {
            await _respawner.ResetAsync(_dbConnection);
            await _dbConnection.DisposeAsync();
        }

        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Scoped bir DbContext olusturur. Her cagrida yeni scope + yeni context.
    /// </summary>
    protected IntegrationDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        return factory.CreateDbContext();
    }

    /// <summary>
    /// Scoped servis resolve eder. Dikkat: scope yonetimi caller'a aittir.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Scoped servis resolve eder ve scope ile birlikte doner.
    /// Caller scope'u dispose etmelidir.
    /// </summary>
    protected (T Service, IServiceScope Scope) GetScopedService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<T>();
        return (service, scope);
    }

    // ─── Phase 0: Shared Seed Helper Methods ─────────────────────────────

    /// <summary>
    /// Product + ProductVariant + BranchOfficeStock olusturur.
    /// BranchOffice, Brand, Category onceden seed edilmis olmali.
    /// </summary>
    protected async Task<(Guid ProductId, Guid VariantId)> SeedProductWithStockAsync(
        string barcode, int stock, int branchOfficeId = 1, int? brandId = null, int? categoryId = null,
        string? stockCode = null)
    {
        using var dbContext = CreateDbContext();

        // Default brand/category: ilk kayitlari al
        brandId ??= await dbContext.Brands.Select(b => b.Id).FirstAsync();
        categoryId ??= await dbContext.Categories.Select(c => c.Id).FirstAsync();

        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        dbContext.MainProducts.Add(new Product
        {
            Id = productId,
            Title = $"Test Product {barcode}",
            Description = "Integration test product",
            StockCode = stockCode ?? $"SC-{barcode}",
            BrandId = brandId,
            CategoryId = categoryId.Value,
            CreatedAt = DateTimeOffset.UtcNow
        });

        dbContext.ProductVariants.Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Barcode = barcode,
            ListPrice = 200,
            SalePrice = 180,
            CurrencyType = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        });

        dbContext.BranchOfficeStocks.Add(new BranchOfficeStock
        {
            BranchOfficeId = branchOfficeId,
            ProductVariantId = variantId,
            FirstTotalStock = stock,
            SoldQuantity = 0
        });

        await dbContext.SaveChangesAsync();
        return (productId, variantId);
    }

    /// <summary>
    /// MarketPlace kaydı seed eder. Respawn her test class'ta temizler.
    /// BaseUrl varsayilan olarak WireMock.BaseUrl — boylece runtime'da marketplace
    /// HTTP client'lari (TrendyolApiClient, HepsiburadaApiClient vb.) gercek API
    /// yerine WireMock in-process server'a yonelir. Override etmek icin baseUrl
    /// parametresi gecilebilir.
    /// </summary>
    protected async Task SeedMarketPlaceAsync(int id, string name, string? baseUrl = null)
    {
        using var dbContext = CreateDbContext();
        if (!await dbContext.MarketPlaces.AnyAsync(mp => mp.Id == id))
        {
            dbContext.MarketPlaces.Add(new MarketPlace
            {
                Id = id,
                Name = name,
                BaseUrl = baseUrl ?? WireMock.BaseUrl,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// MarketPlaceWarehouse kaydı seed eder. Sipariş import stok dusme icin gerekli.
    /// </summary>
    protected async Task SeedMarketPlaceWarehouseAsync(int marketPlaceId, int branchOfficeId)
    {
        using var dbContext = CreateDbContext();
        var exists = await dbContext.MarketPlaceWarehouses
            .AnyAsync(w => w.MarketPlaceId == marketPlaceId && w.BranchOfficeId == branchOfficeId);
        if (!exists)
        {
            dbContext.MarketPlaceWarehouses.Add(new MarketPlaceWarehouse
            {
                MarketPlaceId = marketPlaceId,
                BranchOfficeId = branchOfficeId
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// SaleManager testleri icin Customer + ApplicationUser seed eder.
    /// </summary>
    protected async Task<(Guid UserId, int CustomerId)> SeedCustomerAndUserAsync()
    {
        using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            Name = "Test",
            Surname = "User",
            FullName = "Test User",
            Email = $"test-{userId:N}@test.com",
            UserName = $"testuser-{userId:N}",
            NormalizedUserName = $"TESTUSER-{userId:N}",
            NormalizedEmail = $"TEST-{userId:N}@TEST.COM",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        dbContext.Customers.Add(new Customer
        {
            Name = "Test",
            Surname = "Customer",
            FullName = "Test Customer",
            CustomerType = "Retail",
            PhoneNumber = "5551234567",
            Address = new Address
            {
                City = "Istanbul",
                Country = "Turkey",
                FullAddress = "Test Adres"
            },
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var customerId = await dbContext.Customers
            .Where(c => c.FullName == "Test Customer")
            .Select(c => c.Id)
            .FirstAsync();

        return (userId, customerId);
    }

    /// <summary>
    /// Ortak seed: BranchOffice, Brand, Category. Cogu test class'in ihtiyaci var.
    /// </summary>
    protected async Task SeedBasicEntitiesAsync(
        string brandName = "Test Marka",
        string categoryName = "Test Kategori",
        int branchOfficeId = 1)
    {
        using var dbContext = CreateDbContext();

        if (!await dbContext.BranchOffices.AnyAsync(b => b.Id == branchOfficeId))
        {
            dbContext.BranchOffices.Add(new BranchOffice
            {
                Id = branchOfficeId,
                Name = "Ana Depo",
                IsDefaultMarketPlaceStock = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (!await dbContext.Brands.AnyAsync(b => b.Name == brandName))
        {
            dbContext.Brands.Add(new Brand
            {
                Name = brandName,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (!await dbContext.Categories.AnyAsync(c => c.Name == categoryName))
        {
            dbContext.Categories.Add(new Category
            {
                Name = categoryName,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
