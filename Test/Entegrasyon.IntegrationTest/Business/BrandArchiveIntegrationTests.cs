using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// BrandService.DeleteBrand ürün guard + RestoreBrand — gerçek persist doğrulaması.
///
/// EF Mutasyon Persist strict-rule: RestoreBrand global no-tracking altında AsTracking ile
/// yüklediği için değişiklik DB'ye GERÇEKTEN yazılmalı. AsTracking olmadan restore RED olur
/// (IsDeleted=false persist olmaz). DeleteBrand guard'ı ürünü olan markada SaveChanges'i
/// hiç çağırmadan ErrorResult döner; ürünsüz markada soft-delete DB'ye yazılır.
/// </summary>
[Trait("Category", "Integration")]
public class BrandArchiveIntegrationTests : IntegrationTestBase
{
    public BrandArchiveIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private async Task<int> SeedBrandAsync(string name, bool isDeleted = false)
    {
        using var db = CreateDbContext();
        var brand = new Brand
        {
            Name = name,
            NormalizedName = name.Trim().ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : default
        };
        db.Brands.Add(brand);
        await db.SaveChangesAsync();
        return brand.Id;
    }

    private async Task SeedProductForBrandAsync(int brandId)
    {
        using var db = CreateDbContext();
        var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();
        db.MainProducts.Add(new Product
        {
            Id = Guid.NewGuid(),
            Title = "Guard Test Product",
            Description = "Integration test product",
            StockCode = $"SC-{Guid.NewGuid():N}",
            BrandId = brandId,
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private IBrandService Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IBrandService>();
        scope = s;
        return svc;
    }

    [Fact]
    public async Task DeleteBrand_WhenBrandHasProduct_DoesNotDeleteAndReturnsError()
    {
        // Arrange — markaya bağlı görünür ürün var → guard silmeyi engellemeli.
        var brandId = await SeedBrandAsync("Guarded");
        await SeedProductForBrandAsync(brandId);

        var sut = Sut(out var scope);
        using var _ = scope;

        // Act
        var result = await sut.DeleteBrand(brandId);

        // Assert — hata döner + marka DB'de hâlâ aktif (IsDeleted=false).
        result.Success.Should().BeFalse();

        using var verifyDb = CreateDbContext();
        var persisted = await verifyDb.Brands.IgnoreQueryFilters().FirstAsync(b => b.Id == brandId);
        persisted.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBrand_WhenBrandHasNoProduct_PersistsSoftDelete()
    {
        // Arrange — ürünsüz marka → soft-delete DB'ye yazılmalı.
        var brandId = await SeedBrandAsync("Orphanless");

        var sut = Sut(out var scope);
        using var _ = scope;

        // Act
        var result = await sut.DeleteBrand(brandId);

        // Assert
        result.Success.Should().BeTrue();

        using var verifyDb = CreateDbContext();
        var persisted = await verifyDb.Brands.IgnoreQueryFilters().FirstAsync(b => b.Id == brandId);
        persisted.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task RestoreBrand_SetsIsDeletedFalseAndDeletedAtDefault_PersistsToDatabase()
    {
        // Arrange — soft-deleted marka seed et.
        var brandId = await SeedBrandAsync("Restorable", isDeleted: true);

        var sut = Sut(out var scope);
        using var _ = scope;

        // Act
        var result = await sut.RestoreBrand(brandId);

        // Assert — değişiklik DB'de GERÇEKTEN var (ayrı context'ten oku; AsTracking footgun kanıtı).
        result.Success.Should().BeTrue();

        using var verifyDb = CreateDbContext();
        var persisted = await verifyDb.Brands.IgnoreQueryFilters().FirstAsync(b => b.Id == brandId);
        persisted.IsDeleted.Should().BeFalse();
        persisted.DeletedAt.Should().Be(default);
    }
}
