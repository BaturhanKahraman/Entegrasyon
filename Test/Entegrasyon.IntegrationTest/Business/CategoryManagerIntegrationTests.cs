using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// CategoryManager integration testleri — gercek PostgreSQL ile kategori CRUD akislari.
/// </summary>
[Trait("Category", "Integration")]
public class CategoryManagerIntegrationTests : IntegrationTestBase
{
    public CategoryManagerIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task AddCategoryStepOne_ShouldCreateCategory_InDatabase()
    {
        // Arrange
        var (categoryService, scope) = GetScopedService<ICategoryService>();
        using var _ = scope;
        var dto = new AddCategoryDtoStepOne("Test Kategori", null, false);

        // Act
        var result = await categoryService.AddCategoryStepOne(dto);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == "Test Kategori");
        category.Should().NotBeNull();
        category!.IsFavorite.Should().BeFalse();
        category.SuperCategoryId.Should().BeNull();
    }

    [Fact]
    public async Task AddCategoryStepOne_ShouldReturnError_WhenDuplicateName()
    {
        // Arrange — once ilk kategoriyi ekle
        var (categoryService, scope) = GetScopedService<ICategoryService>();
        using var _ = scope;

        var dto = new AddCategoryDtoStepOne("Tekrar Eden Kategori", null, false);
        var firstResult = await categoryService.AddCategoryStepOne(dto);
        firstResult.Success.Should().BeTrue(firstResult.Message);

        // Act — ayni isimle tekrar ekle
        var (categoryService2, scope2) = GetScopedService<ICategoryService>();
        using var _2 = scope2;
        var duplicateResult = await categoryService2.AddCategoryStepOne(dto);

        // Assert
        duplicateResult.Success.Should().BeFalse("Duplicate category name should be rejected");
    }

    [Fact]
    public async Task AddCategory_ShouldCreateCategory_WithAttributes()
    {
        // Arrange
        var (categoryService, scope) = GetScopedService<ICategoryService>();
        using var _ = scope;

        var dto = new AddCategoryDto(
            Name: "Ozellikli Kategori",
            CategoryAttributes: [],
            SuperCategoryId: null,
            IsFavorite: true
        );

        // Act
        var result = await categoryService.AddCategory(dto);

        // Assert
        result.Success.Should().BeTrue(result.Message);
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Ozellikli Kategori");
    }

    [Fact]
    public async Task SoftDelete_ShouldFail_WhenCategoryHasProducts()
    {
        // Arrange — kategori olustur
        var (categoryService, scope) = GetScopedService<ICategoryService>();
        using var _ = scope;

        var createResult = await categoryService.AddCategoryStepOne(
            new AddCategoryDtoStepOne("Silinecek Kategori", null, false));
        createResult.Success.Should().BeTrue(createResult.Message);

        using var dbContext = CreateDbContext();
        var category = await dbContext.Categories.FirstAsync(c => c.Name == "Silinecek Kategori");

        // Bir brand ve product ekle — foreign key tanimla
        var brand = new Entity.Brands.Brand { Name = "Test Brand", CreatedAt = DateTimeOffset.UtcNow };
        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync();

        var product = new Entity.Products.Product
        {
            Title = "Test Urun",
            Description = "Test",
            StockCode = "CAT-DEL-TEST-001",
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MainProducts.Add(product);
        await dbContext.SaveChangesAsync();

        // Act — urunu olan kategoriyi silmeyi dene
        var (categoryService2, scope2) = GetScopedService<ICategoryService>();
        using var _2 = scope2;
        var deleteResult = await categoryService2.SoftDelete(category.Id);

        // Assert
        deleteResult.Success.Should().BeFalse("Category with products should not be deletable");
    }

    [Fact]
    public async Task SoftDelete_ShouldSucceed_WhenCategoryIsEmpty()
    {
        // Arrange
        var (categoryService, scope) = GetScopedService<ICategoryService>();
        using var _ = scope;

        var createResult = await categoryService.AddCategoryStepOne(
            new AddCategoryDtoStepOne("Bos Kategori", null, false));
        createResult.Success.Should().BeTrue(createResult.Message);

        using var dbContext = CreateDbContext();
        var category = await dbContext.Categories.FirstAsync(c => c.Name == "Bos Kategori");

        // Act
        var (categoryService2, scope2) = GetScopedService<ICategoryService>();
        using var _2 = scope2;
        var deleteResult = await categoryService2.SoftDelete(category.Id);

        // Assert
        deleteResult.Success.Should().BeTrue(deleteResult.Message);
    }

    [Fact]
    public async Task GetLeafCategoriesWithParentAsync_ReturnsOnlyLeafCategories_WithParentName()
    {
        // Arrange — parent + leaf oluştur
        using var dbContext = CreateDbContext();
        var parent = new Category { Name = "ParentCat" };
        await dbContext.Categories.AddAsync(parent);
        await dbContext.SaveChangesAsync();

        var leaf = new Category { Name = "LeafCat", SuperCategoryId = parent.Id };
        var orphanParent = new Category { Name = "OrphanLeaf" }; // parent'sız leaf
        await dbContext.Categories.AddRangeAsync(leaf, orphanParent);
        await dbContext.SaveChangesAsync();

        // Act
        var (categoryService, scope) = GetScopedService<ICategoryService>();
        using var _ = scope;
        var result = await categoryService.GetLeafCategoriesWithParentAsync();

        // Assert
        result.Should().NotContain(x => x.Id == parent.Id); // parent dışarıda
        var leafDto = result.Should().ContainSingle(x => x.Id == leaf.Id).Subject;
        leafDto.ParentName.Should().Be("ParentCat");
        var orphanDto = result.Should().ContainSingle(x => x.Id == orphanParent.Id).Subject;
        orphanDto.ParentName.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryDetailList_ShouldReturnCreatedCategories()
    {
        // Arrange
        var (categoryService, scope) = GetScopedService<ICategoryService>();
        using var _ = scope;

        await categoryService.AddCategoryStepOne(new AddCategoryDtoStepOne("Listeleme Test 1", null, false));
        await categoryService.AddCategoryStepOne(new AddCategoryDtoStepOne("Listeleme Test 2", null, true));

        // Act
        var (categoryService2, scope2) = GetScopedService<ICategoryService>();
        using var _2 = scope2;
        var result = await categoryService2.GetCategoryDetailList();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().Contain(c => c.Name == "Listeleme Test 1");
        result.Data!.Should().Contain(c => c.Name == "Listeleme Test 2");
    }

    [Fact]
    public async Task UpdateCategory_ThrowsOrReturnsError_WhenRowVersionStale()
    {
        // Arrange — seed category via DbContext (sibling test pattern)
        using var dbContext = CreateDbContext();
        var cat = new Category { Name = "OriginalName" };
        dbContext.Categories.Add(cat);
        await dbContext.SaveChangesAsync();

        // İlk form yüklendiğindeki stale RowVersion (gerçek xmin asla 0 olmaz)
        var staleRowVersion = 0u;

        var dto = new EditCategoryDto(cat.Id, "UpdatedName", null, false, false, null, staleRowVersion);

        // Act
        var (categoryService, scope) = GetScopedService<ICategoryService>();
        using var _ = scope;
        var result = await categoryService.UpdateCategory(dto);

        // Assert — xmin 0 olmayacağından concurrency hatası bekleniyor
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("değiştirildi");
    }
}
