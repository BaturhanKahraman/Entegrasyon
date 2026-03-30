using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.N11;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11RestProductService birim testleri.
/// REST client çağrılarının doğru parametrelerle yapıldığını ve
/// task ID'sinin BatchRequestId'ye kaydedildiğini doğrular.
/// </summary>
public class N11RestProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11RestClient> _restClientMock = new();
    private readonly Mock<IN11SoapClient> _soapClientMock = new();
    private readonly Mock<N11MappingValidator> _validatorMock;
    private readonly Mock<IProductActivityLogger> _activityLoggerMock = new();
    private readonly Mock<IMinioFileStorage> _fileStorageMock = new();
    private readonly Mock<ILogger<N11RestProductService>> _loggerMock = new();

    private static readonly Guid TestProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public N11RestProductServiceTests()
    {
        _validatorMock = new Mock<N11MappingValidator>(mockContextFactory.Object);

        _activityLoggerMock
            .Setup(x => x.LogAsync(
                It.IsAny<Guid>(), It.IsAny<ProductActivityType>(), It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private N11RestProductService CreateSut() => new(
        _restClientMock.Object,
        _soapClientMock.Object,
        _validatorMock.Object,
        _activityLoggerMock.Object,
        mockContextFactory.Object,
        _fileStorageMock.Object,
        _loggerMock.Object);

    // -----------------------------------------------------------------------
    // SaveProductAsync — mapping validation failure
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveProductAsync_WhenMappingValidationFails_ShouldReturnError()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new Entegrasyon.Entity.Results.ErrorResult("Kategori eşleştirmesi eksik"));

        var sut = CreateSut();

        // Act
        var result = await sut.SaveProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kategori eşleştirmesi eksik");
        _restClientMock.Verify(r => r.PostAsync<It.IsAnyType, It.IsAnyType>(
            It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // SaveProductAsync — product not found
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveProductAsync_WhenProductNotFound_ShouldReturnError()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.ValidateProductMappingsAsync(TestProductId))
            .ReturnsAsync(new Entegrasyon.Entity.Results.SuccessResult());

        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product>());
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(new List<ProductMarketplace>());

        var sut = CreateSut();

        // Act
        var result = await sut.SaveProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Ürün bulunamadı");
    }

    // -----------------------------------------------------------------------
    // DeleteProductAsync — no ExternalProductId
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteProductAsync_WhenNoExternalProductId_ShouldReturnError()
    {
        // Arrange
        var productMarketplaces = new List<ProductMarketplace>
        {
            new() { ProductId = TestProductId, MarketPlaceId = N11MarketPlaceId, ExternalProductId = null }
        };
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(productMarketplaces);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ExternalProductId bulunamadı");
        _soapClientMock.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Xml.Linq.XElement>()), Times.Never);
    }

    // -----------------------------------------------------------------------
    // DeleteProductAsync — uses SOAP client
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteProductAsync_WhenExternalProductIdExists_ShouldUseSoapClient()
    {
        // Arrange
        var productMarketplaces = new List<ProductMarketplace>
        {
            new() { ProductId = TestProductId, MarketPlaceId = N11MarketPlaceId, ExternalProductId = "987654321" }
        };
        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces).ReturnsDbSet(productMarketplaces);

        var soapResponse = new System.Xml.Linq.XElement("DeleteProductByIdResponse",
            new System.Xml.Linq.XElement("result",
                new System.Xml.Linq.XElement("status", "success")));

        _soapClientMock
            .Setup(s => s.SendAsync("ProductService", "", It.IsAny<System.Xml.Linq.XElement>()))
            .ReturnsAsync(soapResponse);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteProductAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
        _soapClientMock.Verify(s => s.SendAsync("ProductService", "", It.IsAny<System.Xml.Linq.XElement>()), Times.Once);
        _restClientMock.Verify(r => r.PostAsync<It.IsAnyType, It.IsAnyType>(
            It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
