using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// CategoryAttributeValueManager — GetOrCreate (kanonik dedup), UpdateName (persist + NormalizedName
/// yeniden hesap) ve SoftDelete (filtreli unique index ile kanonik yeniden kullanım) davranışlarını
/// gerçek PostgreSQL (Testcontainers) üzerinde doğrular.
/// </summary>
[Trait("Category", "Integration")]
[Collection(Entegrasyon.IntegrationTest.Collections.IntegrationTestCollection.Name)]
public class CategoryAttributeValueDedupIntegrationTests : IntegrationTestBase
{
    public CategoryAttributeValueDedupIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task GetOrCreate_SameCanonical_ReturnsSameId_NoDuplicate()
    {
        // Arrange — CategoryAttribute seed
        int attrId;
        using (var db = CreateDbContext())
        {
            var attr = new CategoryAttribute
            {
                CategoryAttributeKey = "renk",
                CategoryAttributeHumanized = "Renk",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.CategoryAttributes.Add(attr);
            await db.SaveChangesAsync();
            attrId = attr.Id;
        }

        var (manager, scope) = GetScopedService<ICategoryAttributeValueManager>();
        using var _ = scope;

        // Act
        var id1 = await manager.GetOrCreate(attrId, "Sarı");
        var id2 = await manager.GetOrCreate(attrId, "  SARI ");   // same canonical → same id
        var id3 = await manager.GetOrCreate(attrId, "Kırmızı");   // different canonical → new id

        // Assert: same canonical → same id
        Assert.Equal(id1, id2);
        // Assert: different canonical → different id
        Assert.NotEqual(id1, id3);

        // Assert: exactly 2 active values exist for this attrId
        using var db2 = CreateDbContext();
        var activeCount = await db2.CategoryAttributeValues
            .CountAsync(v => v.CategoryAttributeId == attrId && !v.IsDeleted);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task UpdateName_Persists_AndRecomputesNormalized()
    {
        // Arrange
        int attrId;
        using (var db = CreateDbContext())
        {
            var attr = new CategoryAttribute
            {
                CategoryAttributeKey = "renk",
                CategoryAttributeHumanized = "Renk",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.CategoryAttributes.Add(attr);
            await db.SaveChangesAsync();
            attrId = attr.Id;
        }

        var (manager, scope) = GetScopedService<ICategoryAttributeValueManager>();
        using var _ = scope;
        var valId = await manager.GetOrCreate(attrId, "Sari");

        // Act
        await manager.UpdateName(valId, attrId, "Sarı");

        // Assert — persist + NormalizedName yeniden hesaplanmış (no-tracking footgun testi)
        using var db2 = CreateDbContext();
        var v = await db2.CategoryAttributeValues.FirstAsync(x => x.Id == valId);
        Assert.Equal("Sarı", v.Name);
        Assert.Equal("SARI", v.NormalizedName);
    }

    [Fact]
    public async Task SoftDelete_MarksDeleted_AndFreesCanonicalForReuse()
    {
        // Arrange
        int attrId;
        using (var db = CreateDbContext())
        {
            var attr = new CategoryAttribute
            {
                CategoryAttributeKey = "renk",
                CategoryAttributeHumanized = "Renk",
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.CategoryAttributes.Add(attr);
            await db.SaveChangesAsync();
            attrId = attr.Id;
        }

        var (manager, scope) = GetScopedService<ICategoryAttributeValueManager>();
        using var _ = scope;
        var firstId = await manager.GetOrCreate(attrId, "Sarı");

        // Act — soft-delete, sonra aynı kanonik tekrar eklenebilmeli (filtreli unique index IsDeleted=false)
        await manager.SoftDelete(firstId, attrId);
        var secondId = await manager.GetOrCreate(attrId, "Sarı");

        // Assert
        Assert.NotEqual(firstId, secondId);
        using var db2 = CreateDbContext();
        var deleted = await db2.CategoryAttributeValues.IgnoreQueryFilters().FirstAsync(x => x.Id == firstId);
        Assert.True(deleted.IsDeleted);
        var active = await db2.CategoryAttributeValues.CountAsync(v => v.CategoryAttributeId == attrId && !v.IsDeleted);
        Assert.Equal(1, active);
    }
}
