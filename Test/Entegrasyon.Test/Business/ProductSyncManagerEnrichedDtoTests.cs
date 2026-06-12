using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class ProductSyncManagerEnrichedDtoTests : BaseTest
{
    private readonly ProductSyncManager _sut;
    private readonly Mock<IProductActivityLogger> _mockActivityLogger = new();

    public ProductSyncManagerEnrichedDtoTests()
    {
        mockTenantContext.Setup(t => t.TenantId).Returns(1);

        _sut = new ProductSyncManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            _mockActivityLogger.Object,
            mockTenantContext.Object);
    }

    [Fact]
    public async Task GetProductSyncDetailAsync_ShouldIncludeEnrichedFields_WhenMarketplaceRecordExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "Test Ürün",
            StockCode = "TST-001",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var marketplace = new MarketPlace { Id = 1, Name = "Trendyol" };

        var pm = new ProductMarketplace
        {
            Id = 1,
            ProductId = productId,
            MarketPlaceId = 1,
            Status = MarketplaceProductStatus.Published,
            ExternalProductId = "EXT-123",
            ContentId = 456789L,
            IsApproved = true,
            IsArchived = false,
            LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-1),
            BatchRequestId = "BATCH-001"
        };
        product.ProductMarketplaces = new List<ProductMarketplace> { pm };
        product.ProductVariants = new List<ProductVariant>();

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { marketplace });

        // Act
        var result = await _sut.GetProductSyncDetailAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var trendyolItem = result.Data.Marketplaces.FirstOrDefault(m => m.MarketPlaceId == 1);
        trendyolItem.Should().NotBeNull();
        trendyolItem!.ExternalProductId.Should().Be("EXT-123");
        trendyolItem.ContentId.Should().Be(456789L);
        trendyolItem.IsApproved.Should().BeTrue();
        trendyolItem.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductSyncDetailAsync_ShouldReturnNullEnrichedFields_WhenNoMarketplaceRecord()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "Test Ürün",
            StockCode = "TST-001",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        product.ProductMarketplaces = new List<ProductMarketplace>();
        product.ProductVariants = new List<ProductVariant>();

        var marketplace = new MarketPlace { Id = 1, Name = "Trendyol" };

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product> { product });
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace> { marketplace });

        // Act
        var result = await _sut.GetProductSyncDetailAsync(productId);

        // Assert
        result.Success.Should().BeTrue();
        var trendyolItem = result.Data.Marketplaces.FirstOrDefault(m => m.MarketPlaceId == 1);
        trendyolItem.Should().NotBeNull();
        trendyolItem!.ExternalProductId.Should().BeNull();
        trendyolItem.ContentId.Should().BeNull();
        trendyolItem.IsApproved.Should().BeNull();
        trendyolItem.IsArchived.Should().BeNull();
    }

    [Fact]
    public async Task GetProductSyncDetailAsync_ShouldReturnError_WhenProductNotFound()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet(new List<Product>());
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<MarketPlace>());

        // Act
        var result = await _sut.GetProductSyncDetailAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }
}
