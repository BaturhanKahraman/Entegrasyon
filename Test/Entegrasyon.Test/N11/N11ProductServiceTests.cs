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
/// N11ProductService için birim testleri.
/// Validator → Mapper → SoapClient pipeline'ının doğru çalıştığını ve
/// hata durumlarında beklenen sonuçların döndürüldüğünü doğrular.
/// </summary>
public class N11ProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11SoapClient> _soapClientMock = new();
    private readonly Mock<IN11ProductMapper> _productMapperMock = new();
    private readonly Mock<N11MappingValidator> _validatorMock;
    private readonly Mock<IProductActivityLogger> _activityLoggerMock = new();
    private readonly Mock<ILogger<N11ProductService>> _loggerMock = new();

    private static readonly Guid TestProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string TestExternalProductId = "987654321";

    public N11ProductServiceTests()
    {
        _validatorMock = new Mock<N11MappingValidator>(mockContextFactory.Object);

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

    private N11ProductService CreateSut() => new(
        _soapClientMock.Object,
        _productMapperMock.Object,
        _validatorMock.Object,
        _activityLoggerMock.Object,
        mockContextFactory.Object,
        _loggerMock.Object);

    private static XElement BuildSuccessSaveResponse(long n11ProductId = 111222333)
    {
        return new XElement("SaveProductResponse",
            new XElement("result",
                new XElement("status", "success"),
                new XElement("errorCode"),
                new XElement("errorMessage")),
            new XElement("product",
                new XElement("id", n11ProductId)));
    }

    private static XElement BuildFailureResponse(string errorMessage = "N11 hata mesajı")
    {
        return new XElement("SaveProductResponse",
            new XElement("result",
                new XElement("status", "failure"),
                new XElement("errorCode", "ERR-001"),
                new XElement("errorMessage", errorMessage)));
    }

    private static XElement BuildSuccessSimpleResponse(string elementName = "DeleteProductResponse")
    {
        return new XElement(elementName,
            new XElement("result",
                new XElement("status", "success"),
                new XElement("errorMessage")));
    }

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
            Description = "Test açıklaması",
            ProductVariants =
            [
                new ProductVariant
                {
                    Id = variantId,
                    Barcode = "1234567890123",
                    ListPrice = 200m,
                    SalePrice = 150m,
                    BranchOfficeStocks =
                    [
                        new BranchOfficeStock { BranchOfficeId = 1, CurrentStock = 10 }
                    ]
                }
            ]
        };

        mockIntegrationDbContext
            .Setup(x => x.MainProducts)
            .ReturnsDbSet([product]);
    }

    // -----------------------------------------------------------------------
    // Test 1: SaveProductAsync — validator, mapper ve soap sırayla çağrılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveProductAsync_ShouldCallValidatorMapperAndSoap()
    {
        // Arrange
        var productXml = new XElement("product", new XElement("title", "Test"));
        var n11ProductId = 111222333L;

        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new SuccessResult());

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<XElement>(productXml));

        _soapClientMock
            .Setup(s => s.SendAsync("ProductService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildSuccessSaveResponse(n11ProductId));

        mockIntegrationDbContext
            .Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.SaveProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(n11ProductId);

        _validatorMock.Verify(v => v.ValidateProductMappingsAsync(TestProductId), Times.Once);
        _productMapperMock.Verify(m => m.MapProductAsync(TestProductId), Times.Once);
        _soapClientMock.Verify(s => s.SendAsync("ProductService", "", It.IsAny<XElement>()), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Test 2: SaveProductAsync — N11 failure response döndüğünde ErrorDataResult dön
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveProductAsync_WhenResponseFailure_ShouldReturnError()
    {
        // Arrange
        var errorMessage = "Ürün zaten mevcut";
        var productXml = new XElement("product", new XElement("title", "Test"));

        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new SuccessResult());

        _productMapperMock
            .Setup(m => m.MapProductAsync(TestProductId))
            .ReturnsAsync(new SuccessDataResult<XElement>(productXml));

        _soapClientMock
            .Setup(s => s.SendAsync("ProductService", "", It.IsAny<XElement>()))
            .ReturnsAsync(BuildFailureResponse(errorMessage));

        var sut = CreateSut();

        // Act
        var result = await sut.SaveProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(errorMessage);
    }

    // -----------------------------------------------------------------------
    // Test 3: DeleteProductAsync — ExternalProductId SOAP request içinde gönderilir
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteProductAsync_ShouldCallSoapWithExternalId()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(TestExternalProductId);

        XElement? capturedRequest = null;
        _soapClientMock
            .Setup(s => s.SendAsync("ProductService", "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((_, _, req) => capturedRequest = req)
            .ReturnsAsync(BuildSuccessSimpleResponse("DeleteProductResponse"));

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Descendants("productId").FirstOrDefault()?.Value
            .Should().Be(TestExternalProductId);
    }

    // -----------------------------------------------------------------------
    // Test 4: StartSellingAsync — ProductSellingService WSDL kullanılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task StartSellingAsync_ShouldUseProductSellingService()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(TestExternalProductId);

        string? capturedWsdlPath = null;
        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, _) => capturedWsdlPath = wsdl)
            .ReturnsAsync(BuildSuccessSimpleResponse("StartSellingResponse"));

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.StartSellingAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        capturedWsdlPath.Should().Be("ProductSellingService");
    }

    // -----------------------------------------------------------------------
    // Test 5: StopSellingAsync — ProductSellingService WSDL kullanılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task StopSellingAsync_ShouldUseProductSellingService()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(TestExternalProductId);

        string? capturedWsdlPath = null;
        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, _) => capturedWsdlPath = wsdl)
            .ReturnsAsync(BuildSuccessSimpleResponse("StopSellingResponse"));

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.StopSellingAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        capturedWsdlPath.Should().Be("ProductSellingService");
    }

    // -----------------------------------------------------------------------
    // Test 6: DeleteProductAsync — ExternalProductId yoksa ErrorResult dön
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteProductAsync_WhenNoExternalId_ShouldReturnError()
    {
        // Arrange
        SetupProductMarketplaceWithExternalId(null);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        _soapClientMock.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<XElement>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // Test 7: UpdateProductBasicAsync — ProductService WSDL kullanılır
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateProductBasicAsync_ShouldUseProductService()
    {
        // Arrange
        SetupProductWithVariants();
        SetupProductMarketplaceWithExternalId(TestExternalProductId);

        string? capturedWsdlPath = null;
        _soapClientMock
            .Setup(s => s.SendAsync(It.IsAny<string>(), "", It.IsAny<XElement>()))
            .Callback<string, string, XElement>((wsdl, _, _) => capturedWsdlPath = wsdl)
            .ReturnsAsync(BuildSuccessSimpleResponse("UpdateProductBasicResponse"));

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateProductBasicAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        capturedWsdlPath.Should().Be("ProductService");
    }
}
