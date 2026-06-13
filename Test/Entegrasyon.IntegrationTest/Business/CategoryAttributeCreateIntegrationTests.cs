using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// CategoryAttributeManager.AddCategoryAttribute — standalone özellik oluşturma akışını gerçek
/// PostgreSQL (Testcontainers) üzerinde doğrular: kanonik değer dedup + NormalizedName persist
/// (filtreli unique index (CategoryAttributeId, NormalizedName) boş string ile patlamamalı)
/// ve duplicate anahtar reddi.
/// </summary>
[Trait("Category", "Integration")]
[Collection(Entegrasyon.IntegrationTest.Collections.IntegrationTestCollection.Name)]
public class CategoryAttributeCreateIntegrationTests : IntegrationTestBase
{
    public CategoryAttributeCreateIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private static AddCategoryAttributeDto BuildDto(string key, params string[] valueNames) =>
        new(0, false, false, key, false, key,
            valueNames.Select(n => new CategoryAttributeValue { Name = n }).ToList());

    [Fact]
    public async Task AddCategoryAttribute_PersistsAttributeAndDedupedNormalizedValues()
    {
        // Arrange
        var (manager, scope) = GetScopedService<ICategoryAttributeManager>();
        using var _ = scope;

        // Act — "Sarı" / "  SARI " kanonik aynı → tek değer; NormalizedName dolu persist edilmeli
        var result = await manager.AddCategoryAttribute(BuildDto("Renk", "Sarı", "  SARI ", "Kırmızı"));

        // Assert
        Assert.True(result.Success, result.Message);

        using var db = CreateDbContext();
        var attr = await db.CategoryAttributes
            .Include(x => x.CategoryAttributeValues)
            .SingleAsync(x => x.CategoryAttributeKey == "Renk");

        var activeValues = attr.CategoryAttributeValues.Where(v => !v.IsDeleted).ToList();
        Assert.Equal(2, activeValues.Count);
        Assert.All(activeValues, v => Assert.False(string.IsNullOrEmpty(v.NormalizedName)));
        Assert.Contains(activeValues, v => v.NormalizedName == "SARI");
        Assert.Contains(activeValues, v => v.NormalizedName == "KIRMIZI");
    }

    [Fact]
    public async Task AddCategoryAttribute_DuplicateKey_ReturnsError_NoInsert()
    {
        // Arrange — mevcut "beden" anahtarı
        var (manager, scope) = GetScopedService<ICategoryAttributeManager>();
        using var _ = scope;
        var first = await manager.AddCategoryAttribute(BuildDto("beden", "M"));
        Assert.True(first.Success, first.Message);

        // Act — trim + case-insensitive çakışma
        var result = await manager.AddCategoryAttribute(BuildDto(" Beden ", "L"));

        // Assert
        Assert.False(result.Success);

        using var db = CreateDbContext();
        var count = await db.CategoryAttributes
            .CountAsync(x => x.CategoryAttributeKey!.Trim().ToLower() == "beden");
        Assert.Equal(1, count);
    }
}
