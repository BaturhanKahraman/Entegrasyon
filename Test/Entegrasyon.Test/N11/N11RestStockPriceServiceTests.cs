using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11RestStockPriceService birim testleri.
/// price-stock-update endpoint çağrılarının doğru parametrelerle yapıldığını doğrular.
/// </summary>
public class N11RestStockPriceServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11RestClient> _restClientMock = new();
    private readonly Mock<IProductActivityLogger> _activityLoggerMock = new();
    private readonly Mock<ILogger<N11RestStockPriceService>> _loggerMock = new();

    private static readonly Guid TestProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public N11RestStockPriceServiceTests()
    {
        _activityLoggerMock
            .Setup(x => x.LogAsync(
                It.IsAny<Guid>(), It.IsAny<ProductActivityType>(), It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private N11RestStockPriceService CreateSut() => new(
        _restClientMock.Object,
        mockContextFactory.Object,
        _activityLoggerMock.Object,
        _loggerMock.Object);

    private void SetupProductWithVariant(decimal listPrice = 200m, decimal salePrice = 150m)
    {
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Barcode = "SKU001",
            ListPrice = listPrice,
            SalePrice = salePrice,
            BranchOfficeStocks = []
        };

        var product = new Product
        {
            Id = TestProductId,
            ProductVariants = [variant]
        };

        var pm = new ProductMarketplace
        {
            ProductId = TestProductId,
            MarketPlaceId = N11MarketPlaceId,
            ExternalProductId = "123456"
        };

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet([product]);
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet([pm]);
    }

    // -----------------------------------------------------------------------
    // UpdatePriceAsync — no PM record
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePriceAsync_WhenNoProductMarketplace_ShouldReturnError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(new List<ProductMarketplace>());
        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAsync(TestProductId, 150m);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ProductMarketplace kaydı bulunamadı");
        _restClientMock.Verify(r => r.PostAsync<It.IsAnyType, It.IsAnyType>(
            It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // UpdatePriceAsync — REST call made
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePriceAsync_WhenValid_ShouldCallPriceStockUpdateEndpoint()
    {
        // Arrange
        SetupProductWithVariant(listPrice: 200m, salePrice: 150m);

        var taskResponse = new N11TaskResponse(Id: 5001, Type: "SKU_UPDATE", Status: "IN_QUEUE", Reasons: null);

        _restClientMock
            .Setup(r => r.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
                "ms/product/tasks/price-stock-update",
                It.IsAny<N11PriceStockUpdateRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(taskResponse);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAsync(TestProductId, 150m);

        // Assert
        result.Success.Should().BeTrue();
        _restClientMock.Verify(r => r.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
            "ms/product/tasks/price-stock-update",
            It.Is<N11PriceStockUpdateRequest>(req =>
                req.Payload.Skus.Count == 1 &&
                req.Payload.Skus[0].StockCode == "SKU001" &&
                req.Payload.Skus[0].ListPrice != null &&
                req.Payload.Skus[0].SalePrice != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // UpdateStockAsync — REST call with quantity only
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateStockAsync_WhenValid_ShouldCallPriceStockUpdateWithQuantityOnly()
    {
        // Arrange
        SetupProductWithVariant();

        var taskResponse = new N11TaskResponse(Id: 5002, Type: "SKU_UPDATE", Status: "IN_QUEUE", Reasons: null);

        _restClientMock
            .Setup(r => r.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
                "ms/product/tasks/price-stock-update",
                It.IsAny<N11PriceStockUpdateRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(taskResponse);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStockAsync(TestProductId, 25);

        // Assert
        result.Success.Should().BeTrue();
        _restClientMock.Verify(r => r.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
            "ms/product/tasks/price-stock-update",
            It.Is<N11PriceStockUpdateRequest>(req =>
                req.Payload.Skus.Count == 1 &&
                req.Payload.Skus[0].Quantity == 25 &&
                req.Payload.Skus[0].ListPrice == null &&
                req.Payload.Skus[0].SalePrice == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // UpdatePriceAsync — REST returns null
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePriceAsync_WhenRestReturnsNull_ShouldReturnError()
    {
        // Arrange
        SetupProductWithVariant();

        _restClientMock
            .Setup(r => r.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
                It.IsAny<string>(), It.IsAny<N11PriceStockUpdateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((N11TaskResponse?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAsync(TestProductId, 150m);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("REST API yanıt vermedi");
    }

    // -----------------------------------------------------------------------
    // listPrice >= salePrice rule
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePriceAsync_WhenNewPriceHigherThanListPrice_ShouldCapAtListPrice()
    {
        // Arrange
        SetupProductWithVariant(listPrice: 100m, salePrice: 80m);

        N11PriceStockUpdateRequest? capturedRequest = null;
        _restClientMock
            .Setup(r => r.PostAsync<N11PriceStockUpdateRequest, N11TaskResponse>(
                It.IsAny<string>(), It.IsAny<N11PriceStockUpdateRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, N11PriceStockUpdateRequest, CancellationToken>((_, req, _) => capturedRequest = req)
            .ReturnsAsync(new N11TaskResponse(Id: 1, Type: "SKU_UPDATE", Status: "IN_QUEUE", Reasons: null));

        var sut = CreateSut();

        // newPrice=150 > listPrice=100 → salePrice should be capped to listPrice=100
        await sut.UpdatePriceAsync(TestProductId, 150m);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Payload.Skus[0].SalePrice.Should().Be(100m);
    }
}
