using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiMappingValidator unit testleri.
/// </summary>
public class CiceksepetiMappingValidatorTests : Entegrasyon.UnitTest.BaseTest
{
    private CiceksepetiMappingValidator CreateSut() => new(mockContextFactory.Object);

    private static Product BuildProduct(Guid id, int categoryId, string title = "Test Ürün") =>
        new()
        {
            Id = id,
            CategoryId = categoryId,
            Title = title
        };

    // ── Test 1 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_MissingCategoryMatch_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { BuildProduct(productId, categoryId) });

        // No CategoryMarketplace for MarketPlaceId=8
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("kategori");
    }

    // ── Test 2 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_MissingRequiredAttribute_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { BuildProduct(productId, categoryId) });

        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = CiceksepetiMarketPlaceId, MarketPlaceCategoryId = 500 }
            });

        // Two required attributes
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>
            {
                new() { CategoryId = categoryId, CategoryAttributeId = 100, IsRequired = true },
                new() { CategoryId = categoryId, CategoryAttributeId = 101, IsRequired = true }
            });

        // Only one is mapped — 101 is missing
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>
            {
                new() { ApplicationCategoryAttributeId = 100, MarketPlaceId = CiceksepetiMarketPlaceId, MarketPlaceCategoryAttributeId = 200 }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("özellik");
    }

    // ── Test 3 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_AllMappingsComplete_ReturnsSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { BuildProduct(productId, categoryId) });

        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = CiceksepetiMarketPlaceId, MarketPlaceCategoryId = 500 }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>
            {
                new() { CategoryId = categoryId, CategoryAttributeId = 100, IsRequired = true }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>
            {
                new() { ApplicationCategoryAttributeId = 100, MarketPlaceId = CiceksepetiMarketPlaceId, MarketPlaceCategoryAttributeId = 200 }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
    }
}
