using System.Net;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class TrendyolProductServiceDeleteTests : BaseTest
{
    private readonly TrendyolProductService _sut;
    private readonly Mock<ITrendyolApiClient> _mockApiClient = new();
    private readonly Mock<ITrendyolProductMapper> _mockMapper = new();
    private readonly TrendyolMappingValidator _validator;
    private readonly Mock<IProductActivityLogger> _mockActivityLogger = new();

    private readonly Guid _productId = Guid.NewGuid();
    private readonly int _trendyolMarketPlaceId = 1;

    public TrendyolProductServiceDeleteTests()
    {
        _validator = new TrendyolMappingValidator(mockContextFactory.Object);

        _sut = new TrendyolProductService(
            mockContextFactory.Object,
            _mockApiClient.Object,
            _mockMapper.Object,
            _validator,
            _mockActivityLogger.Object,
            NullLogger<TrendyolProductService>.Instance);

        // Default: logger does nothing
        _mockActivityLogger
            .Setup(l => l.LogAsync(
                It.IsAny<Guid>(), It.IsAny<ProductActivityType>(), It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupProductWithVariants(string barcode = "TEST-BARCODE-001")
    {
        var sellerId = "123456";
        var marketplace = new MarketPlace { Id = _trendyolMarketPlaceId, Name = "Trendyol", SellerId = sellerId };
        var variant = new ProductVariant { Id = Guid.NewGuid(), Barcode = barcode };
        var product = new Product
        {
            Id = _productId,
            Title = "Test Ürün",
            StockCode = "TST-001",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        product.ProductVariants = new List<ProductVariant> { variant };

        var pm = new ProductMarketplace
        {
            Id = 1,
            ProductId = _productId,
            MarketPlaceId = _trendyolMarketPlaceId,
            Status = MarketplaceProductStatus.Published
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace> { marketplace });
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(new List<ProductMarketplace> { pm });
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldReturnSuccess_WhenApiCallSucceeds()
    {
        // Arrange
        SetupProductWithVariants();

        var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _mockApiClient
            .Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(successResponse);

        // Act
        var result = await _sut.DeleteProductAsync(_productId);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldLogDeletedActivity_WhenApiCallSucceeds()
    {
        // Arrange
        SetupProductWithVariants();
        _mockApiClient
            .Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _sut.DeleteProductAsync(_productId);

        // Assert
        _mockActivityLogger.Verify(l => l.LogAsync(
            _productId,
            ProductActivityType.Deleted,
            It.IsAny<string>(),
            ProductActivityStatus.Success,
            It.IsAny<string?>(),
            "Trendyol",
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldReturnError_WhenProductNotFound()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product>());
        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace>());

        // Act
        var result = await _sut.DeleteProductAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldReturnError_WhenApiCallFails()
    {
        // Arrange
        SetupProductWithVariants();

        var errorBody = "{\"errors\":[{\"code\":\"PRODUCT_NOT_FOUND\",\"message\":\"Product not found\"}]}";
        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorBody)
        };
        _mockApiClient
            .Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(errorResponse);

        // Act
        var result = await _sut.DeleteProductAsync(_productId);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldLogErrorActivity_WhenApiCallFails()
    {
        // Arrange
        SetupProductWithVariants();
        _mockApiClient
            .Setup(c => c.DeleteAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"not found\"}")
            });

        // Act
        await _sut.DeleteProductAsync(_productId);

        // Assert
        _mockActivityLogger.Verify(l => l.LogAsync(
            _productId,
            ProductActivityType.Deleted,
            It.IsAny<string>(),
            ProductActivityStatus.Error,
            It.IsAny<string?>(),
            "Trendyol",
            It.IsAny<string?>()), Times.Once);
    }
}
