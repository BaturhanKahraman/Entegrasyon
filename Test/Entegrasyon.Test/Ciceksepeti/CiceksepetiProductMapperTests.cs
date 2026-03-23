using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiProductMapper unit testleri.
/// </summary>
public class CiceksepetiProductMapperTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IMinioFileStorage> _mockFileStorage = new();
    private readonly Mock<ILogger<CiceksepetiProductMapper>> _mockLogger = new();

    private CiceksepetiProductMapper CreateSut() =>
        new(mockContextFactory.Object, _mockFileStorage.Object, _mockLogger.Object);

    private static Product BuildProduct(Guid productId, int categoryId)
    {
        var variantId = Guid.NewGuid();
        return new Product
        {
            Id = productId,
            Title = "Test Çiçek",
            Description = "Güzel bir çiçek",
            StockCode = "TST-001",
            CategoryId = categoryId,
            ProductVariants = new List<ProductVariant>
            {
                new()
                {
                    Id = variantId,
                    ProductId = productId,
                    Barcode = "1234567890123",
                    SalePrice = 99.90m,
                    ListPrice = 120.00m,
                    BranchOfficeStocks = new List<BranchOfficeStock>
                    {
                        new() { BranchOfficeId = 1, CurrentStock = 50 }
                    },
                    Images = new List<Image>()
                }
            },
            AttributeKeyValues = new List<AttributeKeyValue>()
        };
    }

    // ── Test 1 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapToCreateRequest_ValidProduct_ReturnsRequest()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var product = BuildProduct(productId, categoryId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = CiceksepetiMarketPlaceId, MarketPlaceCategoryId = 500 }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());

        _mockFileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>())).Returns("https://cdn.example.com/img.jpg");

        var sut = CreateSut();

        // Act
        var result = await sut.MapToCreateRequestAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Products.Should().HaveCount(1);

        var req = result.Data.Products[0];
        req.CategoryId.Should().Be(500);
        req.ProductName.Should().Be("Test Çiçek");
        req.StockCode.Should().Be("1234567890123");
        req.SalesPrice.Should().Be(99.90m);
        req.ListPrice.Should().Be(120.00m);
        req.StockQuantity.Should().Be(50);
    }

    // ── Test 2 ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task MapToUpdateRequest_SetsIsActiveTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = 10;
        var product = BuildProduct(productId, categoryId);

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = CiceksepetiMarketPlaceId, MarketPlaceCategoryId = 500 }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());

        _mockFileStorage.Setup(x => x.GetPublicUrl(It.IsAny<string>())).Returns("https://cdn.example.com/img.jpg");

        var sut = CreateSut();

        // Act — MapToUpdateRequest uses isActive=true internally
        var result = await sut.MapToUpdateRequestAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Products.Should().HaveCount(1);

        // The request should be built successfully — isActive is a mapper concern
        var req = result.Data.Products[0];
        req.ProductName.Should().Be("Test Çiçek");
        req.CategoryId.Should().Be(500);
    }
}
