using System.Net;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Categories;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Entegrasyon.IntegrationTest.Features.Categories;

/// <summary>
/// Kategori detay sayfasının (GET /categories/{id}) GERÇEK HTTP render yolu.
/// Controller → CategoryDetailPageVm → Detail.cshtml wiring'ini doğrular:
/// 200/404, KPI kartları, özellik tablosu, "Özellik Yönet" + header Düzenle/Sil, satışsız empty state.
/// NOT: Testcontainers/PostgreSQL gerektirir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CategoryDetailRenderHttpTests : IntegrationTestBase
{
    private int _leafCategoryId;

    public CategoryDetailRenderHttpTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private static async Task<string> GetHtmlAsync(HttpClient client, string url)
        => WebUtility.HtmlDecode(await client.GetStringAsync(url));

    /// <summary>
    /// Global AutoValidateAntiforgeryToken aktif. HTMX prod'da token'ı meta[csrf-token]'dan
    /// RequestVerificationToken header'ına koyar (site.js); test HttpClient JS koşmaz, o yüzden
    /// header'ı elle ekliyoruz. detayfrom GET ile meta token'ı çekip POST'a iliştirir.
    /// </summary>
    private async Task<HttpResponseMessage> PostWithCsrfAsync(
        HttpClient client, string detailUrl, string postUrl, HttpContent? content)
    {
        var page = await client.GetStringAsync(detailUrl);
        var m = System.Text.RegularExpressions.Regex.Match(
            page, "name=\"csrf-token\" content=\"([^\"]+)\"");
        m.Success.Should().BeTrue("layout antiforgery meta token içermeli");

        var req = new HttpRequestMessage(HttpMethod.Post, postUrl) { Content = content };
        req.Headers.Add("RequestVerificationToken", m.Groups[1].Value);
        return await client.SendAsync(req);
    }

    protected override IntegrationTestWebAppFactory CreateFactory(string connectionString)
        => new CategoryDetailWebAppFactory(connectionString);

    // TestAuthHandler'ın kimliği — AddLog ApplicationUserId FK'sini karşılamak için seed edilmeli.
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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

        var cat = new Category { Name = "Bebek Tulumu", CreatedAt = DateTimeOffset.UtcNow };
        db.Categories.Add(cat);
        var attr = new CategoryAttribute
        {
            CategoryAttributeKey = "Renk",
            CategoryAttributeHumanized = "Renk",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.CategoryAttributes.Add(attr);
        await db.SaveChangesAsync();

        db.CategoryAttributeCategories.Add(new CategoryAttributeCategory
        {
            CategoryId = cat.Id,
            CategoryAttributeId = attr.Id,
            IsRequired = true,
            IsVarianter = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        _leafCategoryId = cat.Id;
    }

    [Fact]
    public async Task Detail_returns_404_when_category_missing()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/categories/987654");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Detail_renders_meta_and_kpi_cards()
    {
        var client = CreateClient();

        var html = await GetHtmlAsync(client, $"/categories/{_leafCategoryId}");

        html.Should().Contain("Bebek Tulumu");
        // KPI başlıkları (satış adedi, ciro, sipariş, iade, ortalama fiyat)
        html.Should().Contain("Satış Adedi");
        html.Should().Contain("Ciro");
        html.Should().Contain("Sipariş");
        html.Should().Contain("İade");
        html.Should().Contain("Ortalama");
    }

    [Fact]
    public async Task Detail_renders_attribute_table_with_edit_link()
    {
        var client = CreateClient();

        var html = await GetHtmlAsync(client, $"/categories/{_leafCategoryId}");

        html.Should().Contain("Renk");                                          // bağlı özellik adı
        html.Should().Contain($"/categories/{_leafCategoryId}/attributes");     // özellik sayfası linki
        html.Should().Contain("Özellikleri Düzenle");
    }

    [Fact]
    public async Task Detail_renders_actions_dropdown_with_edit_attributes_and_disabled_marketplace()
    {
        var client = CreateClient();

        var html = await GetHtmlAsync(client, $"/categories/{_leafCategoryId}");

        html.Should().Contain("İşlemler");                                   // dropdown toggle
        html.Should().Contain($"/categories/{_leafCategoryId}/edit");        // Düzenle
        html.Should().Contain("Özellikleri Düzenle");
        html.Should().Contain("Pazaryeri Ayarları");
        html.Should().Contain("Yakında");                                    // disabled placeholder
        html.Should().Contain("Sil");
    }

    [Fact]
    public async Task Detail_renders_empty_state_when_no_sales()
    {
        var client = CreateClient();

        var html = await GetHtmlAsync(client, $"/categories/{_leafCategoryId}");

        // Seed'de hiç sipariş yok → satış bölümü empty state göstermeli
        html.Should().Contain("empty");
        html.Should().Contain("Henüz");
    }

    // ── Attributes page (ayrı tam sayfa) ──────────────────────────────

    [Fact]
    public async Task AttributesPage_returns_404_when_category_missing()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/categories/987654/attributes");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AttributesPage_renders_attached_and_addable()
    {
        // Eklenebilir bir havuz özelliği daha ekle (henüz bağlı değil)
        using (var db = CreateDbContext())
        {
            db.CategoryAttributes.Add(new CategoryAttribute
            {
                CategoryAttributeKey = "Beden",
                CategoryAttributeHumanized = "Beden",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        var client = CreateClient();

        var html = await GetHtmlAsync(client, $"/categories/{_leafCategoryId}/attributes");

        html.Should().Contain("Bağlı Özellikler");
        html.Should().Contain("Renk");                                               // bağlı
        html.Should().Contain("Özellik Ekle");
        html.Should().Contain("Beden");                                              // havuzdan eklenebilir
        // ekle/kaldır HTMX endpoint'leri sayfada olmalı
        html.Should().Contain($"/categories/{_leafCategoryId}/attributes");
    }

    [Fact]
    public async Task AddAttribute_attaches_pool_attribute_and_returns_updated_panel()
    {
        int bedenId;
        using (var db = CreateDbContext())
        {
            var attr = new CategoryAttribute
            {
                CategoryAttributeKey = "Beden",
                CategoryAttributeHumanized = "Beden",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.CategoryAttributes.Add(attr);
            await db.SaveChangesAsync();
            bedenId = attr.Id;
        }
        var client = CreateClient();

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("attributeIds", bedenId.ToString()),
            new KeyValuePair<string, string>("requiredId", bedenId.ToString())
        });
        var response = await PostWithCsrfAsync(client,
            $"/categories/{_leafCategoryId}/attributes",
            $"/categories/{_leafCategoryId}/attributes", form);

        response.IsSuccessStatusCode.Should().BeTrue();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        html.Should().Contain("Beden");
        html.Should().Contain("Zorunlu");

        // DB'de bağ oluşmuş olmalı
        using var verifyDb = CreateDbContext();
        var attached = await verifyDb.CategoryAttributeCategories
            .AnyAsync(x => x.CategoryId == _leafCategoryId && x.CategoryAttributeId == bedenId);
        attached.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAttribute_detaches_and_returns_updated_panel()
    {
        // _leafCategoryId'ye bağlı "Renk" özelliğinin id'sini bul
        int renkId;
        using (var db = CreateDbContext())
            renkId = (await db.CategoryAttributeCategories
                .FirstAsync(x => x.CategoryId == _leafCategoryId)).CategoryAttributeId;

        var client = CreateClient();

        var response = await PostWithCsrfAsync(client,
            $"/categories/{_leafCategoryId}/attributes",
            $"/categories/{_leafCategoryId}/attributes/{renkId}/remove", content: null);

        response.IsSuccessStatusCode.Should().BeTrue();

        using var db2 = CreateDbContext();
        var stillAttached = await db2.CategoryAttributeCategories
            .AnyAsync(x => x.CategoryId == _leafCategoryId && x.CategoryAttributeId == renkId);
        stillAttached.Should().BeFalse("kaldırılan özellik artık bağlı görünmemeli");
    }
}

/// <summary>Kimlik doğrulamalı (TestAuthHandler → [Authorize] geçer) detay render factory.</summary>
file sealed class CategoryDetailWebAppFactory : IntegrationTestWebAppFactory
{
    private readonly string _connectionString;

    public CategoryDetailWebAppFactory(string connectionString) : base(connectionString)
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
                new CategoryDetailStubTenantRegistry(_connectionString));

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

file sealed class CategoryDetailStubTenantRegistry(string connectionString) : ITenantRegistryDataSource
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
