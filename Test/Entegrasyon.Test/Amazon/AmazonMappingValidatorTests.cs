using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonMappingValidator unit tests.
/// </summary>
public class AmazonMappingValidatorTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ILogger<AmazonMappingValidator>> _mockLogger = new();

    private AmazonMappingValidator CreateSut() => new(
        mockContextFactory.Object,
        _mockLogger.Object);

    private static Product BuildProduct(Guid id, int categoryId, int? brandId = 1,
        List<ProductVariant>? variants = null)
    {
        return new Product
        {
            Id = id,
            CategoryId = categoryId,
            Title = "Test Product",
            BrandId = brandId,
            ProductVariants = variants ?? new List<ProductVariant>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = id,
                    Barcode = "8680123456789",
                    Images = new List<Image> { new() { Id = 1, StorageKey = "test.jpg" } }
                }
            }
        };
    }

    // ── Test 1 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_ProductNotFound_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    // ── Test 2 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_NoVariants_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildProduct(productId, 10, variants: new List<ProductVariant>());

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("varyant");
    }

    // ── Test 3 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_NoBrand_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildProduct(productId, 10, brandId: null);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("marka");
    }

    // ── Test 4 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_VariantWithoutBarcode_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variants = new List<ProductVariant>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Barcode = null, // Missing barcode
                Images = new List<Image> { new() { Id = 1, StorageKey = "test.jpg" } }
            }
        };
        var product = BuildProduct(productId, 10, variants: variants);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("barkod");
    }

    // ── Test 5 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_VariantWithoutImages_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variants = new List<ProductVariant>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Barcode = "8680123456789",
                Images = new List<Image>() // No images
            }
        };
        var product = BuildProduct(productId, 10, variants: variants);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("görsel");
    }

    // ── Test 6 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_MissingCategoryMapping_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildProduct(productId, 10);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>()); // No Amazon category mapping

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("kategori");
    }

    // ── Test 7 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_AllValid_ReturnsSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildProduct(productId, 10);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = 10, MarketPlaceId = AmazonMarketPlaceId, MarketPlaceCategoryName = "SHOES" }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
    }

    // ── Test 8 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Validate_MultipleErrors_ReturnsAllErrorsConcatenated()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variants = new List<ProductVariant>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Barcode = null, // Missing barcode
                Images = new List<Image>() // Missing images
            }
        };
        var product = BuildProduct(productId, 10, brandId: null, variants: variants);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        // Should contain multiple errors separated by " | "
        result.Message.Should().Contain("|");
        result.Message.Should().Contain("marka");
        result.Message.Should().Contain("barkod");
    }
}
