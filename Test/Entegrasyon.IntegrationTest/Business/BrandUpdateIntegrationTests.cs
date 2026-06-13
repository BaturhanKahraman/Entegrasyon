using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// BrandService.UpdateBrand — gercek persist dogrulamasi (no-tracking footgun).
///
/// Global no-tracking yuzunden entity LINQ ile yuklenip mutate edilince SaveChanges
/// sessizce no-op olabilir. Bu test, AsTracking ile yuklenen entity'nin SeoSlug
/// degisikliginin DB'ye GERCEKTEN yazildigini ayri bir context'ten okuyarak dogrular.
/// AsTracking olmadan bu test RED olur (degisiklik persist olmaz).
/// </summary>
[Trait("Category", "Integration")]
public class BrandUpdateIntegrationTests : IntegrationTestBase
{
    public BrandUpdateIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private async Task<int> SeedBrandAsync(string name, string? seoSlug = null)
    {
        using var db = CreateDbContext();
        // NormalizedName uygulamadaki/migration backfill'deki formulle (UPPER(TRIM(Name))) ayni —
        // dedup kontrolu artik bu kolon uzerinden, NULL birakirsa cakisma yakalanmaz.
        var brand = new Brand
        {
            Name = name,
            NormalizedName = name.Trim().ToUpperInvariant(),
            SeoSlug = seoSlug,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Brands.Add(brand);
        await db.SaveChangesAsync();
        return brand.Id;
    }

    private IBrandService Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IBrandService>();
        scope = s;
        return svc;
    }

    [Fact]
    public async Task UpdateBrand_OnlySeoSlugChanged_PersistsToDatabase()
    {
        // Arrange — markayi seed et, ardindan yalnizca SeoSlug guncelle (ad ayni kalir).
        var brandId = await SeedBrandAsync("Nike", "nike");

        var sut = Sut(out var scope);
        using var _ = scope;

        // Act
        var result = await sut.UpdateBrand(new EditBrandDto(brandId, "Nike", "nike-turkiye"));

        // Assert — islem basarili + degisiklik DB'de GERCEKTEN var (ayri context'ten oku).
        result.Success.Should().BeTrue();

        using var verifyDb = CreateDbContext();
        var persisted = await verifyDb.Brands.FirstAsync(b => b.Id == brandId);
        persisted.Name.Should().Be("Nike");
        persisted.SeoSlug.Should().Be("nike-turkiye");
    }

    [Fact]
    public async Task UpdateBrand_RenameToOtherExistingBrandName_DoesNotPersistAndReturnsError()
    {
        // Arrange — iki marka; ilkini ikincinin adina cevirmeye calis.
        var nikeId = await SeedBrandAsync("Nike", "nike");
        await SeedBrandAsync("Adidas", "adidas");

        var sut = Sut(out var scope);
        using var _ = scope;

        // Act
        var result = await sut.UpdateBrand(new EditBrandDto(nikeId, "Adidas", "nike-yeni"));

        // Assert — hata doner + Nike kaydi degismeden kalir.
        result.Success.Should().BeFalse();

        using var verifyDb = CreateDbContext();
        var persisted = await verifyDb.Brands.FirstAsync(b => b.Id == nikeId);
        persisted.Name.Should().Be("Nike");
        persisted.SeoSlug.Should().Be("nike");
    }
}
