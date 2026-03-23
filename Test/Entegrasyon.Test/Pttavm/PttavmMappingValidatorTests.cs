using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Pttavm;

/// <summary>
/// PttavmMappingValidator unit testleri.
/// </summary>
public class PttavmMappingValidatorTests : Entegrasyon.UnitTest.BaseTest
{
    private PttavmMappingValidator CreateSut() => new(mockContextFactory.Object);

    private static Product BuildProduct(Guid id, int categoryId, string title = "Test Urun") =>
        new()
        {
            Id = id,
            CategoryId = categoryId,
            Title = title
        };

    // ── Test 1: Urun bulunamazsa hata donmeli ────────────────────────────────

    [Fact]
    public async Task Validate_ProductNotFound_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    // ── Test 2: Kategori eslestirmesi yoksa hata donmeli ─────────────────────

    [Fact]
    public async Task Validate_MissingCategoryMatch_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { BuildProduct(productId, categoryId) });

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

    // ── Test 3: Zorunlu ozellik eslestirmesi eksikse hata donmeli ────────────

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
                new() { CategoryId = categoryId, MarketPlaceId = PttavmMarketPlaceId, MarketPlaceCategoryId = 500 }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>
            {
                new() { CategoryId = categoryId, CategoryAttributeId = 100, IsRequired = true },
                new() { CategoryId = categoryId, CategoryAttributeId = 101, IsRequired = true }
            });

        // Only one mapped
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>
            {
                new() { ApplicationCategoryAttributeId = 100, MarketPlaceId = PttavmMarketPlaceId, MarketPlaceCategoryAttributeId = 200 }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("özellik");
    }

    // ── Test 4: Tum eslestirmeler tamamsa basarili donmeli ───────────────────

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
                new() { CategoryId = categoryId, MarketPlaceId = PttavmMarketPlaceId, MarketPlaceCategoryId = 500 }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>
            {
                new() { CategoryId = categoryId, CategoryAttributeId = 100, IsRequired = true }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>
            {
                new() { ApplicationCategoryAttributeId = 100, MarketPlaceId = PttavmMarketPlaceId, MarketPlaceCategoryAttributeId = 200 }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
    }

    // ── Test 5: Zorunlu ozellik yoksa direkt basarili donmeli ────────────────

    [Fact]
    public async Task Validate_NoRequiredAttributes_ReturnsSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { BuildProduct(productId, categoryId) });

        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = PttavmMarketPlaceId, MarketPlaceCategoryId = 500 }
            });

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
