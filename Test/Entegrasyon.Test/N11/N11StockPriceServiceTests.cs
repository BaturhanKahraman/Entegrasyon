using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11StockPriceService için birim testleri.
/// Fiyat ve stok güncelleme SOAP çağrılarının doğru WSDL ve parametrelerle
/// yapıldığını, hata durumlarında beklenen sonuçların döndürüldüğünü doğrular.
/// </summary>
public class N11StockPriceServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11SoapClient> _soapClientMock = new();
    private readonly Mock<IProductActivityLogger> _activityLoggerMock = new();
    private readonly Mock<ILogger<N11StockPriceService>> _loggerMock = new();

    private static readonly Guid TestProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string TestExternalProductId = "112233445";

    public N11StockPriceServiceTests()
    {
        _activityLoggerMock
            .Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<ProductActivityType>(),
                It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    private N11StockPriceService CreateSut() => new(
        _soapClientMock.Object,
        mockContextFactory.Object,
        _activityLoggerMock.Object,
        _loggerMock.Object);

    private void SetupProductMarketplaceWithExternalId(string? externalId)
    {
        var pmList = externalId is not null
            ? new List<ProductMarketplace>
            {
                new()
                {
                    Id = 1,
                    ProductId = TestProductId,
                    MarketPlaceId = 2,
                    ExternalProductId = externalId,
                    Status = MarketplaceProductStatus.Published
                }
            }
            : new List<ProductMarketplace>();

        mockIntegrationDbContext
            .Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(pmList);
    }

    private void SetupProductWithVariants()
    {
        var variantId = Guid.NewGuid();
        var product = new Product
        {
            Id = TestProductId,
            Title = "Test Ürünü",
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = variantId,
                    Barcode = "9876543210123",
                    ListPrice = 300m,
                    SalePrice = 250m
                }
            ]
        };

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet([product]);
    }

    private static XElement BuildSuccessResponse(string elementName = "UpdatePriceResponse") =>
        new(elementName,
            new XElement("result",
                new XElement("status", "success"),
                new XElement("errorMessage")));

    private static XElement BuildFailureResponse(string errorMessage = "N11 hata mesajı") =>
        new("UpdateResponse",
            new XElement("result",
                new XElement("status", "failure"),
                new XElement("errorCode", "ERR-002"),
                new XElement("errorMessage", errorMessage)));

    // -----------------------------------------------------------------------
    // Test 1: UpdatePriceAsync — ProductService WSDL kullanılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePriceAsync_ShouldCallProductServiceWsdl()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(TestExternalProductId);
        SetupProductWithVariants();

        string? capturedWsdlPath = null;
        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, _) => capturedWsdlPath = wsdl)
            .ReturnsAsync(BuildSuccessResponse("UpdateProductPriceByIdResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAsync(TestProductId, 199m);

        // Assert
        result.Success.Should().BeTrue();
        capturedWsdlPath.Should().Be("ProductService");
    }

    // -----------------------------------------------------------------------
    // Test 2: UpdatePriceAsync — ExternalProductId yoksa ErrorResult dön
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePriceAsync_WhenNoExternalId_ShouldReturnError()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(null);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdatePriceAsync(TestProductId, 199m);

        // Assert
        result.Success.Should().BeFalse();
        _soapClientMock.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<XElement>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // Test 3: UpdateStockAsync — ProductStockService WSDL kullanılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateStockAsync_ShouldCallProductStockServiceWsdl()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(TestExternalProductId);
        SetupProductWithVariants();

        string? capturedWsdlPath = null;
        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, _) => capturedWsdlPath = wsdl)
            .ReturnsAsync(BuildSuccessResponse("UpdateStockBySellerStockCodeResponse"));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStockAsync(TestProductId, 25);

        // Assert
        result.Success.Should().BeTrue();
        capturedWsdlPath.Should().Be("ProductStockService");
    }

    // -----------------------------------------------------------------------
    // Test 4: UpdateStockAsync — N11 failure response döndüğünde ErrorResult dön
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateStockAsync_WhenResponseFailure_ShouldReturnError()
    {
        // Arrange
        var errorMessage = "Stok güncelleme başarısız";
        SetupProductMarketplaceWithExternalId(TestExternalProductId);
        SetupProductWithVariants();

        _soapClientMock
            .Setup(s => s.SendAsync("ProductStockService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildFailureResponse(errorMessage));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateStockAsync(TestProductId, 10);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
    }

    // -----------------------------------------------------------------------
    // Test 5: UpdatePriceAsync — SOAP request içinde fiyat ve barkod doğru gönderilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePriceAsync_ShouldSendCorrectPriceAndBarcode()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(TestExternalProductId);
        SetupProductWithVariants();

        XElement? capturedRequest = null;
        _soapClientMock
            .Setup(s => s.SendAsync("ProductService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse("UpdateProductPriceByIdResponse"));

        var sut = CreateSut();
        const decimal newPrice = 299m;

        // Act
        await sut.UpdatePriceAsync(TestProductId, newPrice);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("productId").FirstOrDefault()?.Value
            .Should().Be(TestExternalProductId);
        capturedRequest.Descendants("price").FirstOrDefault()?.Value
            .Should().Be(newPrice.ToString("F2"));
        capturedRequest.Descendants("sellerStockCode").FirstOrDefault()?.Value
            .Should().Be("9876543210123");
    }

    // -----------------------------------------------------------------------
    // Test 6: UpdateStockAsync — SOAP request içinde sellerStockCode ve quantity doğru gönderilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateStockAsync_ShouldSendCorrectBarcodeAndQuantity()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(TestExternalProductId);
        SetupProductWithVariants();

        XElement? capturedRequest = null;
        _soapClientMock
            .Setup(s => s.SendAsync("ProductStockService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessResponse("UpdateStockBySellerStockCodeResponse"));

        var sut = CreateSut();
        const int quantity = 42;

        // Act
        await sut.UpdateStockAsync(TestProductId, quantity);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("sellerStockCode").FirstOrDefault()?.Value
            .Should().Be("9876543210123");
        capturedRequest.Descendants("quantity").FirstOrDefault()?.Value
            .Should().Be(quantity.ToString());
        capturedRequest.Descendants("version").FirstOrDefault()?.Value
            .Should().Be("0");
    }

    // -----------------------------------------------------------------------
    // Test 7: UpdatePriceAsync — activity log PriceUpdated olarak kaydedilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdatePriceAsync_ShouldLogPriceUpdatedActivity()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(TestExternalProductId);
        SetupProductWithVariants();

        _soapClientMock
            .Setup(s => s.SendAsync("ProductService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildSuccessResponse("UpdateProductPriceByIdResponse"));

        var sut = CreateSut();

        // Act
        await sut.UpdatePriceAsync(TestProductId, 150m);

        // Assert
        _activityLoggerMock.Verify(
            x => x.LogAsync(
                TestProductId,
                ProductActivityType.PriceUpdated,
                It.IsAny<string>(),
                ProductActivityStatus.Success,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }
}
