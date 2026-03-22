using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Test.N11;

public class N11MappingValidatorTests : Entegrasyon.UnitTest.BaseTest
{
    private N11MappingValidator CreateSut() => new(mockContextFactory.Object);

    private static Product BuildProduct(Guid id, int categoryId, int? brandId = 1, string title = "Test Ürün") =>
        new()
        {
            Id = id,
            CategoryId = categoryId,
            BrandId = brandId,
            Title = title
        };

    [Fact]
    public async Task ValidateAsync_WhenAllMappingsExist_ShouldReturnSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var brandId = 5;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId) };

        var categoryMatches = new List<CategoryMarketPlaceMatch>
        {
            new() { ApplicationCategoryId = categoryId, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
        };

        var brandMatches = new List<BrandMarketPlaceMatch>
        {
            new() { ApplicationBrandId = brandId, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
        };

        var requiredAttrs = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = categoryId, CategoryAttributeId = 100, IsRequired = true }
        };

        var attrMatches = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 100, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketPlaceMatches).ReturnsDbSet(categoryMatches);
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches).ReturnsDbSet(brandMatches);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(requiredAttrs);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(attrMatches);

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenCategoryMatchMissing_ShouldReturnError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var brandId = 5;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId) };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryMarketPlaceMatch>()); // no match
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>
            {
                new() { ApplicationBrandId = brandId, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
            });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("N11");
    }

    [Fact]
    public async Task ValidateAsync_WhenBrandMatchMissing_ShouldReturnError()
    {
        // Arrange — BrandId is set but no match exists in BrandMarketPlaceMatches
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var brandId = 5;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId) };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryMarketPlaceMatch>
            {
                new() { ApplicationCategoryId = categoryId, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
            });
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>()); // no match
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("N11");
    }

    [Fact]
    public async Task ValidateAsync_WhenRequiredAttributeMissing_ShouldReturnError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var brandId = 5;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId) };

        var requiredAttrs = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = categoryId, CategoryAttributeId = 100, IsRequired = true },
            new() { CategoryId = categoryId, CategoryAttributeId = 101, IsRequired = true }
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryMarketPlaceMatch>
            {
                new() { ApplicationCategoryId = categoryId, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
            });
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>
            {
                new() { ApplicationBrandId = brandId, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
            });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(requiredAttrs);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>
            {
                // Only one of two required attributes is mapped
                new() { ApplicationCategoryAttributeId = 100, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("N11");
    }

    [Fact]
    public async Task ValidateAsync_WhenBrandIdNull_ShouldReturnSuccess()
    {
        // Arrange — N11-specific: BrandId null is NOT an error
        var productId = Guid.NewGuid();
        var categoryId = 10;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId: null) };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryMarketPlaceMatch>
            {
                new() { ApplicationCategoryId = categoryId, MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId }
            });
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
    }
}
