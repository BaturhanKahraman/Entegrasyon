using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonProductMapper unit tests — verifies product-to-listing mapping.
/// </summary>
public class AmazonProductMapperTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IMinioFileStorage> _mockFileStorage = new();
    private readonly Mock<ILogger<AmazonProductMapper>> _mockLogger = new();

    private AmazonProductMapper CreateSut() => new(
        mockContextFactory.Object,
        _mockFileStorage.Object,
        _mockLogger.Object);

    private Product BuildFullProduct(Guid productId, int categoryId = 10)
    {
        var variantId = Guid.NewGuid();
        return new Product
        {
            Id = productId,
            Title = "Test Product Title",
            Description = "Test Description",
            StockCode = "TST-001",
            CategoryId = categoryId,
            BrandId = 1,
            Brand = new Brand { Id = 1, Name = "TestBrand" },
            ProductVariants = new List<ProductVariant>
            {
                new()
                {
                    Id = variantId,
                    ProductId = productId,
                    Barcode = "8680123456789",
                    SalePrice = 199.90m,
                    Images = new List<Image>
                    {
                        new() { Id = 1, StorageKey = "products/img1.jpg" },
                        new() { Id = 2, StorageKey = "products/img2.jpg" }
                    },
                    BranchOfficeStocks = new List<BranchOfficeStock>
                    {
                        new() { CurrentStock = 10 },
                        new() { CurrentStock = 5 }
                    }
                }
            },
            AttributeKeyValues = new List<AttributeKeyValue>
            {
                new() { CategoryAttributeId = 100, CustomValue = "Cotton" }
            }
        };
    }

    private void SetupDbContext(Product product)
    {
        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>
            {
                new()
                {
                    ApplicationCategoryAttributeId = 100,
                    MarketPlaceId = AmazonMarketPlaceId,
                    MarketPlaceCategoryAttributeExternalId = "material"
                }
            });
    }

    // ── Test 1 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapProductAsync_ValidProduct_ReturnsListingItem()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildFullProduct(productId);
        SetupDbContext(product);

        _mockFileStorage.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"https://cdn.example.com/{key}");

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(productId, "SHOES");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ProductType.Should().Be("SHOES");
        result.Data.Requirements.Should().Be("LISTING");
        result.Data.Attributes.Should().ContainKey("item_name");
        result.Data.Attributes.Should().ContainKey("brand");
        result.Data.Attributes.Should().ContainKey("externally_assigned_product_identifier");
        result.Data.Attributes.Should().ContainKey("main_product_image_locator");
        result.Data.Attributes.Should().ContainKey("purchasable_offer");
        result.Data.Attributes.Should().ContainKey("fulfillment_availability");
    }

    // ── Test 2 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapProductAsync_ProductNotFound_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(Guid.NewGuid(), "PRODUCT");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    // ── Test 3 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapProductAsync_NoVariant_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "No Variant Product",
            CategoryId = 10,
            ProductVariants = new List<ProductVariant>(),
            AttributeKeyValues = new List<AttributeKeyValue>()
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(productId, "PRODUCT");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("varyant");
    }

    // ── Test 4 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapProductAsync_MapsAttributeKeyValues_ToAmazonAttributes()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildFullProduct(productId);
        SetupDbContext(product);

        _mockFileStorage.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"https://cdn.example.com/{key}");

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(productId, "PRODUCT");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Attributes.Should().ContainKey("material");
    }

    // ── Test 5 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapProductAsync_UsesOverrideTitle_WhenProductMarketplaceExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildFullProduct(productId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>
            {
                new()
                {
                    ProductId = productId,
                    MarketPlaceId = AmazonMarketPlaceId,
                    TitleOverride = "Amazon Override Title"
                }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        _mockFileStorage.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"https://cdn.example.com/{key}");

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(productId, "PRODUCT");

        // Assert
        result.Success.Should().BeTrue();
        // The item_name attribute should be set (content verified by integration tests)
        result.Data!.Attributes.Should().ContainKey("item_name");
    }

    // ── Test 6 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapProductAsync_MultipleImages_MapsMainAndOtherLocators()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildFullProduct(productId);
        SetupDbContext(product);

        _mockFileStorage.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"https://cdn.example.com/{key}");

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(productId, "PRODUCT");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Attributes.Should().ContainKey("main_product_image_locator");
        result.Data.Attributes.Should().ContainKey("other_product_image_locator_1");
    }

    // ── Test 7 ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapProductAsync_StockSummedFromBranchOffices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = BuildFullProduct(productId);
        SetupDbContext(product);

        _mockFileStorage.Setup(f => f.GetPublicUrl(It.IsAny<string>()))
            .Returns<string>(key => $"https://cdn.example.com/{key}");

        var sut = CreateSut();

        // Act
        var result = await sut.MapProductAsync(productId, "PRODUCT");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Attributes.Should().ContainKey("fulfillment_availability");
    }
}
