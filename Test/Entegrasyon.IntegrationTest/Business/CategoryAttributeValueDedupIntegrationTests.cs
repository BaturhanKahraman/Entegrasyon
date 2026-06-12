using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// GetOrCreate — aynı kanonik değerin ikinci çağrısında mevcut kaydın id'sini döndürmesini
/// (dedup) ve farklı değerler için yeni kayıt oluşturduğunu doğrular.
/// NormalizedName kolonu ve unique index migration'ı eklendikten sonra GREEN olacaktır (Task 4+).
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
}
