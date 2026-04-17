using System.Net;
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

public class TrendyolProductServiceUpdateDispatchTests : BaseTest
{
    private readonly TrendyolProductService _sut;
    private readonly Mock<ITrendyolApiClient> _mockApiClient = new();
    private readonly Mock<ITrendyolProductMapper> _mockMapper = new();
    private readonly TrendyolMappingValidator _validator;
    private readonly Mock<IProductActivityLogger> _mockActivityLogger = new();

    private readonly Guid _productId = Guid.NewGuid();
    private const int TrendyolMarketPlaceId = 1;

    public TrendyolProductServiceUpdateDispatchTests()
    {
        _validator = new TrendyolMappingValidator(mockContextFactory.Object);

        _sut = new TrendyolProductService(
            mockContextFactory.Object,
            _mockApiClient.Object,
            _mockMapper.Object,
            _validator,
            _mockActivityLogger.Object,
            NullLogger<TrendyolProductService>.Instance);

        _mockActivityLogger
            .Setup(l => l.LogAsync(
                It.IsAny<Guid>(), It.IsAny<ProductActivityType>(), It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private ProductMarketplace SetupProductMarketplace(bool isApproved, long? contentId = null, MarketplaceProductStatus status = MarketplaceProductStatus.Published)
    {
        var marketplace = new MarketPlace { Id = TrendyolMarketPlaceId, Name = "Trendyol", SellerId = "123456" };
        var pm = new ProductMarketplace
        {
            Id = 1,
            ProductId = _productId,
            MarketPlaceId = TrendyolMarketPlaceId,
            Status = status,
            IsApproved = isApproved,
            ContentId = contentId
        };

        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace> { marketplace });
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(new List<ProductMarketplace> { pm });
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return pm;
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnError_WhenProductNotOnTrendyol()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(new List<ProductMarketplace>());

        // Act
        var result = await _sut.UpdateProductAsync(_productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("gönderilmemiş");
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnError_WhenStatusIsRemoved()
    {
        // Arrange
        SetupProductMarketplace(isApproved: false, status: MarketplaceProductStatus.Removed);

        // Act
        var result = await _sut.UpdateProductAsync(_productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("kaldırılmış");
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldCallUnapprovedUpdate_WhenNotApproved()
    {
        // Arrange
        SetupProductMarketplace(isApproved: false);

        // Unapproved update iç akışı: mapper + API
        var product = new Product
        {
            Id = _productId,
            Title = "Test",
            StockCode = "TST",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        product.ProductVariants = new List<ProductVariant>
        {
            new() { Id = Guid.NewGuid(), Barcode = "BC-001" }
        };
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });

        var mappedData = new TrendyolCreateProductRequest(new List<TrendyolProductItem>());
        _mockMapper
            .Setup(m => m.MapProductAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Entegrasyon.Entity.Results.SuccessDataResult<TrendyolCreateProductRequest>(mappedData));

        _mockApiClient
            .Setup(c => c.PutAsync(It.Is<string>(u => u.Contains("unapproved-bulk-update")), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _sut.UpdateProductAsync(_productId);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(c => c.PutAsync(
            It.Is<string>(u => u.Contains("unapproved-bulk-update")),
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldCallContentUpdate_WhenApproved()
    {
        // Arrange
        SetupProductMarketplace(isApproved: true, contentId: 12345);

        var mappedData = new TrendyolCreateProductRequest(new List<TrendyolProductItem>());
        _mockMapper
            .Setup(m => m.MapProductAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Entegrasyon.Entity.Results.SuccessDataResult<TrendyolCreateProductRequest>(mappedData));

        _mockApiClient
            .Setup(c => c.PutAsync(It.Is<string>(u => u.Contains("content-bulk-update")), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _sut.UpdateProductAsync(_productId);

        // Assert
        result.Success.Should().BeTrue();
        _mockApiClient.Verify(c => c.PutAsync(
            It.Is<string>(u => u.Contains("content-bulk-update")),
            It.IsAny<object>()), Times.Once);
    }
}
