using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Amazon;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonProductService unit tests — publish, update, delete orchestration.
/// </summary>
public class AmazonProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IAmazonListingService> _mockListingService = new();
    private readonly Mock<IAmazonProductMapper> _mockMapper = new();
    private readonly Mock<AmazonMappingValidator> _mockValidator;
    private readonly Mock<IProductActivityLogger> _mockActivityLogger = new();
    private readonly Mock<ILogger<AmazonProductService>> _mockLogger = new();

    public AmazonProductServiceTests()
    {
        _mockValidator = new Mock<AmazonMappingValidator>(mockContextFactory.Object, new Mock<ILogger<AmazonMappingValidator>>().Object);

        _mockActivityLogger
            .Setup(x => x.LogAsync(
                It.IsAny<Guid>(), It.IsAny<ProductActivityType>(), It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private AmazonProductService CreateSut() => new(
        mockContextFactory.Object,
        _mockListingService.Object,
        _mockMapper.Object,
        _mockValidator.Object,
        _mockActivityLogger.Object,
        _mockLogger.Object);

    private void SetupDbForPublish(Guid productId, int categoryId = 10,
        string? stockCode = "TST-001", string? sellerId = "SELLER1")
    {
        var product = new Product
        {
            Id = productId,
            CategoryId = categoryId,
            Title = "Test Product",
            StockCode = stockCode
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });

        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>
            {
                new() { Id = AmazonMarketPlaceId, Name = "Amazon", SellerId = sellerId }
            });

        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = AmazonMarketPlaceId, MarketPlaceCategoryName = "SHOES" }
            });
    }

    // ── PublishProductAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task PublishProductAsync_ValidationFails_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new ErrorResult("Marka belirlenmemiş."));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Marka");
        _mockMapper.Verify(x => x.MapProductAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishProductAsync_MappingFails_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        SetupDbForPublish(productId);

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        _mockMapper
            .Setup(x => x.MapProductAsync(productId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ErrorDataResult<AmazonListingItem>(null!, "Varyant yok."));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Varyant");
    }

    [Fact]
    public async Task PublishProductAsync_ListingAccepted_ReturnsSku()
    {
        // Arrange
        var productId = Guid.NewGuid();
        SetupDbForPublish(productId, stockCode: "MY-SKU");

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        var listingItem = new AmazonListingItem("SHOES", "LISTING", new Dictionary<string, object>());
        _mockMapper
            .Setup(x => x.MapProductAsync(productId, "SHOES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<AmazonListingItem>(listingItem));

        _mockListingService
            .Setup(x => x.PutListingItemAsync("SELLER1", "MY-SKU", listingItem, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<AmazonListingSubmissionResponse>(
                new AmazonListingSubmissionResponse("MY-SKU", "ACCEPTED", "sub-123", null)));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("MY-SKU");
    }

    [Fact]
    public async Task PublishProductAsync_ListingInvalid_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        SetupDbForPublish(productId);

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        var listingItem = new AmazonListingItem("SHOES", "LISTING", new Dictionary<string, object>());
        _mockMapper
            .Setup(x => x.MapProductAsync(productId, "SHOES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<AmazonListingItem>(listingItem));

        _mockListingService
            .Setup(x => x.PutListingItemAsync(It.IsAny<string>(), It.IsAny<string>(), listingItem, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<AmazonListingSubmissionResponse>(
                new AmazonListingSubmissionResponse("TST-001", "INVALID", null,
                    new List<AmazonListingIssue>
                    {
                        new("ERR_001", "Missing required attribute: item_name", "ERROR", null)
                    })));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishProductAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("INVALID");
        result.Message.Should().Contain("item_name");
    }

    // ── CheckListingStatusAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CheckListingStatusAsync_DelegatesToListingService()
    {
        // Arrange
        var expectedResponse = new AmazonListingItemResponse("TEST-SKU", null, null, null, null);
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>
            {
                new() { Id = AmazonMarketPlaceId, Name = "Amazon", SellerId = "SELLER1" }
            });

        _mockListingService
            .Setup(x => x.GetListingItemAsync("SELLER1", "TEST-SKU", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<AmazonListingItemResponse>(expectedResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.CheckListingStatusAsync("TEST-SKU");

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Sku.Should().Be("TEST-SKU");
    }

    // ── DeleteProductAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteProductAsync_NoExternalProductId_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>
            {
                new() { ProductId = productId, MarketPlaceId = AmazonMarketPlaceId, ExternalProductId = null }
            });

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("listing bulunamadı");
    }

    [Fact]
    public async Task DeleteProductAsync_WithExternalProductId_DelegatesToListingService()
    {
        // Arrange
        var productId = Guid.NewGuid();
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>
            {
                new() { ProductId = productId, MarketPlaceId = AmazonMarketPlaceId, ExternalProductId = "MY-SKU" }
            });
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>
            {
                new() { Id = AmazonMarketPlaceId, Name = "Amazon", SellerId = "SELLER1" }
            });

        _mockListingService
            .Setup(x => x.DeleteListingItemAsync("SELLER1", "MY-SKU", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResult("Listing silindi."));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProductAsync_NoProductMarketplace_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
    }

    // ── UpdateProductAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProductAsync_ValidationFails_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new ErrorResult("Barkod eksik."));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateProductAsync(productId);

        // Assert
        result.Success.Should().BeFalse();
    }
}
