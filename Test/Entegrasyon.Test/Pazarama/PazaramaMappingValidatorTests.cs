using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaMappingValidatorTests : Entegrasyon.UnitTest.BaseTest
{
    private PazaramaMappingValidator CreateSut() => new(mockContextFactory.Object);

    private static Product BuildProduct(Guid id, int categoryId, int? brandId = 1, string title = "Test Ürün") =>
        new()
        {
            Id = id,
            CategoryId = categoryId,
            BrandId = brandId,
            Title = title
        };

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenProductNotFound_ShouldReturnError()
    {
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenCategoryMatchMissing_ShouldReturnError()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var brandId = 5;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId) };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>()); // no match
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>
            {
                new() { ApplicationBrandId = brandId, MarketPlaceId = PazaramaMarketPlaceId, MarketPlaceBrandExternalId = "brand-guid-1" }
            });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Pazarama");
        result.Message.Should().Contain("kategorisi");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenBrandIsNull_ShouldReturnError()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId: null) };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = PazaramaMarketPlaceId, ExternalCategoryId = "cat-ext-id" }
            });
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("markası belirlenmemiş");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenBrandMatchMissing_ShouldReturnError()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var brandId = 5;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId) };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = PazaramaMarketPlaceId, ExternalCategoryId = "cat-ext-id" }
            });
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>()); // no match
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Pazarama");
        result.Message.Should().Contain("markası");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenRequiredAttributeMissing_ShouldReturnError()
    {
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
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = PazaramaMarketPlaceId, ExternalCategoryId = "cat-ext-id" }
            });
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>
            {
                new() { ApplicationBrandId = brandId, MarketPlaceId = PazaramaMarketPlaceId, MarketPlaceBrandExternalId = "brand-guid-1" }
            });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(requiredAttrs);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>
            {
                // Only one of two required attributes is mapped
                new() { ApplicationCategoryAttributeId = 100, MarketPlaceId = PazaramaMarketPlaceId, MarketPlaceCategoryAttributeExternalId = "attr-ext-id" }
            });

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Pazarama");
        result.Message.Should().Contain("özellik");
    }

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenAllMappingsExist_ShouldReturnSuccess()
    {
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var brandId = 5;

        var products = new List<Product> { BuildProduct(productId, categoryId, brandId) };

        var requiredAttrs = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = categoryId, CategoryAttributeId = 100, IsRequired = true }
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(products);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = PazaramaMarketPlaceId, ExternalCategoryId = "cat-ext-id" }
            });
        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>
            {
                new() { ApplicationBrandId = brandId, MarketPlaceId = PazaramaMarketPlaceId, MarketPlaceBrandExternalId = "brand-guid-1" }
            });
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(requiredAttrs);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>
            {
                new() { ApplicationCategoryAttributeId = 100, MarketPlaceId = PazaramaMarketPlaceId, MarketPlaceCategoryAttributeExternalId = "attr-ext-id" }
            });

        var sut = CreateSut();
        var result = await sut.ValidateProductMappingsAsync(productId);

        result.Success.Should().BeTrue();
    }
}
