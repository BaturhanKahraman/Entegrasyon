using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Kategori import integration testleri — BaseCategoryImporterService uzerinden gercek DB ile
/// kategori olusturma, hierarchy, marketplace link ve guncelleme akislari dogrulanir.
/// TrendyolCategoryImporter resolve edilir (DI'da kayitli concrete).
/// </summary>
[Trait("Category", "Integration")]
public class CategoryImportIntegrationTests : IntegrationTestBase
{
    public CategoryImportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();

        // Seed MarketPlace Trendyol (id=1) — importer buna bagli
        var marketPlace = new MarketPlace
        {
            Id = 1,
            Name = "Trendyol",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.MarketPlaces.Add(marketPlace);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ImportCategory_ShouldCreate_WithMarketplaceLink()
    {
        // Arrange
        var importer = GetService<TrendyolCategoryImporter>();

        var request = new ExternalCategoryImportRequest
        {
            ExternalId = "EXT-CAT-101",
            Name = "Elektronik"
        };

        // Act
        var result = await importer.ImportCategoryAsync(request);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.ExternalCategoryId == "EXT-CAT-101");

        category.Should().NotBeNull();
        category!.Name.Should().Be("Elektronik");
        category.IsImported.Should().BeTrue();
        category.ImportSource.Should().Be(ImportSource.Trendyol);

        // Marketplace link dogrula
        var link = await dbContext.CategoryMarketplaces
            .FirstOrDefaultAsync(cl => cl.CategoryId == category.Id && cl.MarketPlaceId == 1);
        link.Should().NotBeNull("Marketplace link should be created");
        link!.ExternalCategoryId.Should().Be("EXT-CAT-101");
    }

    [Fact]
    public async Task ImportCategories_ShouldCreateHierarchy()
    {
        // Arrange
        var importer = GetService<TrendyolCategoryImporter>();

        var parentRequest = new ExternalCategoryImportRequest
        {
            ExternalId = "EXT-PARENT-201",
            Name = "Giyim",
            Children =
            [
                new ExternalCategoryImportRequest
                {
                    ExternalId = "EXT-CHILD-202",
                    Name = "Erkek Giyim"
                },
                new ExternalCategoryImportRequest
                {
                    ExternalId = "EXT-CHILD-203",
                    Name = "Kadin Giyim"
                }
            ]
        };

        // Act
        var result = await importer.ImportCategoriesAsync([parentRequest]);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var parent = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.ExternalCategoryId == "EXT-PARENT-201");
        parent.Should().NotBeNull();

        var children = await dbContext.Categories
            .Where(c => c.SuperCategoryId == parent!.Id)
            .ToListAsync();

        children.Should().HaveCount(2);
        children.Select(c => c.Name).Should().Contain("Erkek Giyim");
        children.Select(c => c.Name).Should().Contain("Kadin Giyim");
    }

    [Fact]
    public async Task ImportCategory_ShouldUpdate_WhenSameExternalId()
    {
        // Arrange — ilk import
        var importer = GetService<TrendyolCategoryImporter>();

        var request = new ExternalCategoryImportRequest
        {
            ExternalId = "EXT-UPDATE-301",
            Name = "Eski Isim"
        };
        var result1 = await importer.ImportCategoryAsync(request);
        result1.Success.Should().BeTrue(result1.Message);

        // Act — ayni ExternalId ile guncelle
        var importer2 = GetService<TrendyolCategoryImporter>();
        var updateRequest = new ExternalCategoryImportRequest
        {
            ExternalId = "EXT-UPDATE-301",
            Name = "Yeni Isim"
        };
        var result2 = await importer2.ImportCategoryAsync(updateRequest);

        // Assert — guncellenmeli, duplicate olmamalı
        result2.Success.Should().BeTrue(result2.Message);

        using var dbContext = CreateDbContext();
        var categories = await dbContext.Categories
            .Where(c => c.ExternalCategoryId == "EXT-UPDATE-301")
            .ToListAsync();

        categories.Should().HaveCount(1, "Same ExternalId should not create duplicate");
        categories.First().Name.Should().Be("Yeni Isim");
    }

    [Fact]
    public async Task ImportCategories_ShouldHandleEmptyList()
    {
        // Arrange
        var importer = GetService<TrendyolCategoryImporter>();

        // Act — bos liste ile import
        var result = await importer.ImportCategoriesAsync([]);

        // Assert — hata olmamali
        result.Success.Should().BeTrue(result.Message);
    }

    [Fact]
    public async Task ImportCategory_LeafCategory_HasNoChildren()
    {
        // Arrange
        var importer = GetService<TrendyolCategoryImporter>();

        var leafRequest = new ExternalCategoryImportRequest
        {
            ExternalId = "EXT-LEAF-401",
            Name = "Yaprak Kategori"
            // Children bos = leaf
        };

        // Act
        var result = await importer.ImportCategoryAsync(leafRequest);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var leaf = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.ExternalCategoryId == "EXT-LEAF-401");
        leaf.Should().NotBeNull();

        var children = await dbContext.Categories
            .Where(c => c.SuperCategoryId == leaf!.Id)
            .ToListAsync();
        children.Should().BeEmpty("Leaf category should have no children");
    }
}
