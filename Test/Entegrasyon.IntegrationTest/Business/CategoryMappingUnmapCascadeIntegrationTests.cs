using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Unmap (kategori↔pazaryeri eşleme silme) orphan-safe cascade — gerçek PostgreSQL ile.
/// RED-first persist kanıtı: eşleme kaldırılınca yalnız bu kategoriye özgü (orphan) özellik ve
/// değer eşleşmeleri silinir; BAŞKA hâlâ-eşli kategoriyle PAYLAŞILAN özellik eşleşmesi KORUNUR.
/// </summary>
[Trait("Category", "Integration")]
public class CategoryMappingUnmapCascadeIntegrationTests : IntegrationTestBase
{
    private const int Trendyol = 1;

    public CategoryMappingUnmapCascadeIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        using var db = CreateDbContext();
        db.MarketPlaces.Add(new MarketPlace { Id = Trendyol, Name = "Trendyol", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }

    private async Task<(int CatId, int AttrId, int ValueId)> SeedMappedCategoryWithAttributeAndValueAsync(
        string catName, int mpCategoryId, int mpAttrId, int mpValueId)
    {
        using var db = CreateDbContext();

        var cat = new Category { Name = catName, CreatedAt = DateTimeOffset.UtcNow };
        db.Categories.Add(cat);

        var attr = new CategoryAttribute
        {
            CategoryAttributeKey = $"{catName}-Renk",
            CategoryAttributeHumanized = "Renk",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.CategoryAttributes.Add(attr);
        await db.SaveChangesAsync();

        var value = new CategoryAttributeValue
        {
            Name = "Kırmızı",
            NormalizedName = "KIRMIZI",
            CategoryAttributeId = attr.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.CategoryAttributeValues.Add(value);

        db.CategoryAttributeCategories.Add(new CategoryAttributeCategory
        {
            CategoryId = cat.Id,
            CategoryAttributeId = attr.Id,
            IsRequired = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        db.CategoryMarketplaces.Add(new CategoryMarketplace
        {
            CategoryId = cat.Id,
            MarketPlaceId = Trendyol,
            MarketPlaceCategoryId = mpCategoryId,
            IsActive = true
        });

        await db.SaveChangesAsync();

        db.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
        {
            ApplicationCategoryAttributeId = attr.Id,
            MarketPlaceId = Trendyol,
            MarketPlaceCategoryAttributeId = mpAttrId
        });
        db.CategoryAttributeValueMarketPlaceMatches.Add(new CategoryAttributeValueMarketPlaceMatch
        {
            ApplicationCategoryAttributeValueId = value.Id,
            MarketPlaceId = Trendyol,
            MarketPlaceCategoryAttributeValueId = mpValueId
        });
        await db.SaveChangesAsync();

        return (cat.Id, attr.Id, value.Id);
    }

    [Fact]
    public async Task RemoveCategoryMapping_DeletesOrphanAttributeAndValueMatches()
    {
        // Arrange — tek kategori, özellik yalnız bu kategoriye bağlı
        var (catId, attrId, valueId) =
            await SeedMappedCategoryWithAttributeAndValueAsync("Oyuncak", 100, 348, 1002);
        var (sut, scope) = GetScopedService<ICategoryMatchService>();
        using var _ = scope;

        // Act
        var result = await sut.RemoveCategoryMappingAsync(catId, Trendyol);

        // Assert
        result.Success.Should().BeTrue(result.Message);
        using var db = CreateDbContext();

        (await db.CategoryMarketplaces.AnyAsync(x => x.CategoryId == catId && x.IsActive))
            .Should().BeFalse("kategori eşlemesi silinmeli");
        (await db.CategoryAttributeMarketPlaceMatches
            .AnyAsync(x => x.ApplicationCategoryAttributeId == attrId && x.MarketPlaceId == Trendyol))
            .Should().BeFalse("orphan özellik eşleşmesi cascade ile silinmeli");
        (await db.CategoryAttributeValueMarketPlaceMatches
            .AnyAsync(x => x.ApplicationCategoryAttributeValueId == valueId && x.MarketPlaceId == Trendyol))
            .Should().BeFalse("orphan değer eşleşmesi cascade ile silinmeli");
    }

    [Fact]
    public async Task RemoveCategoryMapping_PreservesSharedAttributeMatch()
    {
        // Arrange — iki leaf kategori AYNI özelliği paylaşır, ikisi de eşli
        using var seedDb = CreateDbContext();
        var cat1 = new Category { Name = "Giyim", CreatedAt = DateTimeOffset.UtcNow };
        var cat2 = new Category { Name = "Ayakkabı", CreatedAt = DateTimeOffset.UtcNow };
        seedDb.Categories.AddRange(cat1, cat2);
        var sharedAttr = new CategoryAttribute
        {
            CategoryAttributeKey = "Shared-Renk",
            CategoryAttributeHumanized = "Renk",
            CreatedAt = DateTimeOffset.UtcNow
        };
        seedDb.CategoryAttributes.Add(sharedAttr);
        await seedDb.SaveChangesAsync();

        seedDb.CategoryAttributeCategories.AddRange(
            new CategoryAttributeCategory { CategoryId = cat1.Id, CategoryAttributeId = sharedAttr.Id, CreatedAt = DateTimeOffset.UtcNow },
            new CategoryAttributeCategory { CategoryId = cat2.Id, CategoryAttributeId = sharedAttr.Id, CreatedAt = DateTimeOffset.UtcNow });
        seedDb.CategoryMarketplaces.AddRange(
            new CategoryMarketplace { CategoryId = cat1.Id, MarketPlaceId = Trendyol, MarketPlaceCategoryId = 200, IsActive = true },
            new CategoryMarketplace { CategoryId = cat2.Id, MarketPlaceId = Trendyol, MarketPlaceCategoryId = 201, IsActive = true });
        await seedDb.SaveChangesAsync();

        seedDb.CategoryAttributeMarketPlaceMatches.Add(new CategoryAttributeMarketPlaceMatch
        {
            ApplicationCategoryAttributeId = sharedAttr.Id,
            MarketPlaceId = Trendyol,
            MarketPlaceCategoryAttributeId = 348
        });
        await seedDb.SaveChangesAsync();

        var (sut, scope) = GetScopedService<ICategoryMatchService>();
        using var _ = scope;

        // Act — sadece cat1'in eşlemesini kaldır
        var result = await sut.RemoveCategoryMappingAsync(cat1.Id, Trendyol);

        // Assert — paylaşılan özellik eşleşmesi cat2 hâlâ eşli olduğu için KORUNMALI
        result.Success.Should().BeTrue(result.Message);
        using var db = CreateDbContext();
        (await db.CategoryAttributeMarketPlaceMatches
            .AnyAsync(x => x.ApplicationCategoryAttributeId == sharedAttr.Id && x.MarketPlaceId == Trendyol))
            .Should().BeTrue("paylaşılan özellik eşleşmesi, başka eşli kategori onu kullandığı için korunmalı");
    }
}
