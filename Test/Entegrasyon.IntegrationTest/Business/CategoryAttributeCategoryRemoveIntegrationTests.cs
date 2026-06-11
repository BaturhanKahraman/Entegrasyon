using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// RemoveCategoryAttributeFromCategory — kategoriden tek özellik kaldırma (junction soft-delete).
/// Gerçek PostgreSQL ile: composite PK + HasQueryFilter(!IsDeleted) etkileşimi, çift loglama ve
/// geri-uyumluluk (kaldırılan özellik tekrar eklenebilmeli — PK çakışması olmamalı) doğrulanır.
/// </summary>
[Trait("Category", "Integration")]
public class CategoryAttributeCategoryRemoveIntegrationTests : IntegrationTestBase
{
    public CategoryAttributeCategoryRemoveIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    /// <summary>Bir leaf kategori + ona bağlı bir özellik (junction) oluşturur, (catId, attrId) döner.</summary>
    private async Task<(int CatId, int AttrId)> SeedCategoryWithAttributeAsync(string catName, string attrKey)
    {
        using var db = CreateDbContext();
        var cat = new Category { Name = catName, CreatedAt = DateTimeOffset.UtcNow };
        db.Categories.Add(cat);
        var attr = new CategoryAttribute
        {
            CategoryAttributeKey = attrKey,
            CategoryAttributeHumanized = attrKey,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.CategoryAttributes.Add(attr);
        await db.SaveChangesAsync();

        db.CategoryAttributeCategories.Add(new CategoryAttributeCategory
        {
            CategoryId = cat.Id,
            CategoryAttributeId = attr.Id,
            IsRequired = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        return (cat.Id, attr.Id);
    }

    [Fact]
    public async Task RemoveCategoryAttributeFromCategory_SoftDeletesJunction_AndHidesIt()
    {
        // Arrange
        var (catId, attrId) = await SeedCategoryWithAttributeAsync("Kaldırma Kategori", "Renk");
        var (sut, scope) = GetScopedService<ICategoryAttributeCategoryManager>();
        using var _ = scope;

        // Act
        var result = await sut.RemoveCategoryAttributeFromCategory(catId, attrId);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        // Query filter (!IsDeleted) → kaldırılan bağ görünmez
        var visible = await db.CategoryAttributeCategories
            .AnyAsync(x => x.CategoryId == catId && x.CategoryAttributeId == attrId);
        visible.Should().BeFalse("soft-delete edilen junction query filter ile gizlenmeli");

        // Soft-delete: satır DB'de hâlâ var ama IsDeleted=true
        var row = await db.CategoryAttributeCategories
            .IgnoreQueryFilters()
            .FirstAsync(x => x.CategoryId == catId && x.CategoryAttributeId == attrId);
        row.IsDeleted.Should().BeTrue();
        row.DeletedAt.Should().BeAfter(default);
    }

    [Fact]
    public async Task RemoveCategoryAttributeFromCategory_WritesUserFacingLog()
    {
        // Arrange
        var (catId, attrId) = await SeedCategoryWithAttributeAsync("Log Kategori", "Beden");
        var (sut, scope) = GetScopedService<ICategoryAttributeCategoryManager>();
        using var _ = scope;

        // Act
        var result = await sut.RemoveCategoryAttributeFromCategory(catId, attrId);

        // Assert — kullanıcı-facing log (IApplicationLogManager) yazılmış olmalı
        result.Success.Should().BeTrue(result.Message);
        using var db = CreateDbContext();
        var loggedDelete = await db.Logs
            .AnyAsync(l => l.LogType == LogType.Category && l.LogAction == LogAction.Delete);
        loggedDelete.Should().BeTrue("özellik kaldırma kullanıcı log'una Category/Delete olarak yazılmalı");
    }

    [Fact]
    public async Task RemoveCategoryAttributeFromCategory_ReturnsError_WhenCategoryDoesNotExist()
    {
        var (sut, scope) = GetScopedService<ICategoryAttributeCategoryManager>();
        using var _ = scope;

        var result = await sut.RemoveCategoryAttributeFromCategory(999999, 1);

        result.Success.Should().BeFalse("var olmayan kategori için hata dönmeli");
    }

    [Fact]
    public async Task RemoveCategoryAttributeFromCategory_ReturnsError_WhenBindingDoesNotExist()
    {
        // Kategori var ama bu attr ona bağlı değil
        using (var db = CreateDbContext())
        {
            db.Categories.Add(new Category { Name = "Bağsız Kategori", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        int catId;
        using (var db = CreateDbContext())
            catId = (await db.Categories.FirstAsync(c => c.Name == "Bağsız Kategori")).Id;

        var (sut, scope) = GetScopedService<ICategoryAttributeCategoryManager>();
        using var _ = scope;

        var result = await sut.RemoveCategoryAttributeFromCategory(catId, 424242);

        result.Success.Should().BeFalse("var olmayan bağ için hata dönmeli");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, -1)]
    public async Task RemoveCategoryAttributeFromCategory_ReturnsError_WhenIdsInvalid(int catId, int attrId)
    {
        var (sut, scope) = GetScopedService<ICategoryAttributeCategoryManager>();
        using var _ = scope;

        var result = await sut.RemoveCategoryAttributeFromCategory(catId, attrId);

        result.Success.Should().BeFalse("geçersiz id'ler validation'da reddedilmeli");
    }

    [Fact]
    public async Task RemoveCategoryAttributeFromCategory_AllowsReAddingSameAttribute()
    {
        // Geri-uyumluluk: kaldırılan özellik panelden tekrar eklenebilmeli.
        // Composite PK (CategoryId, CategoryAttributeId) + soft-deleted satır DB'de durduğundan
        // naif re-add PK çakışması yapar — re-add yolu (AddCategoryAttributeForCategory) hard-delete
        // ile mevcut junction'ları temizlediği için bu çalışmalı.
        var (catId, attrId) = await SeedCategoryWithAttributeAsync("Tekrar Ekle Kategori", "Desen");

        var (sut, scope) = GetScopedService<ICategoryAttributeCategoryManager>();
        using var _ = scope;

        var removeResult = await sut.RemoveCategoryAttributeFromCategory(catId, attrId);
        removeResult.Success.Should().BeTrue(removeResult.Message);

        // Aynı özelliği tekrar ekle
        var addResult = await sut.AddCategoryAttributeForCategory(catId, new[]
        {
            new AddCategoryAttributeDto
            {
                Id = attrId,
                CategoryAttributeKey = "Desen",
                CategoryAttributeHumanized = "Desen",
                IsRequired = false
            }
        });

        addResult.Success.Should().BeTrue("kaldırılan özellik tekrar eklenebilmeli — " + addResult.Message);

        using var db = CreateDbContext();
        var visible = await db.CategoryAttributeCategories
            .AnyAsync(x => x.CategoryId == catId && x.CategoryAttributeId == attrId);
        visible.Should().BeTrue("tekrar eklenen bağ görünür olmalı");
    }
}
