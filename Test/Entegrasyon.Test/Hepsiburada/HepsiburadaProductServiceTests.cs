using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Test.Hepsiburada;

/// <summary>
/// HepsiburadaProductService unit testleri.
/// Gercel API client mock'lanir — publish, status check, pre-match approve islemleri test edilir.
/// </summary>
public class HepsiburadaProductServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHepsiburadaApiClient> _mockApiClient = new();
    private readonly Mock<IHepsiburadaProductMapper> _mockMapper = new();
    private readonly Mock<HepsiburadaMappingValidator> _mockValidator;
    private readonly Mock<IProductActivityLogger> _mockActivityLogger = new();
    private readonly Mock<ILogger<HepsiburadaProductService>> _mockLogger = new();

    public HepsiburadaProductServiceTests()
    {
        _mockValidator = new Mock<HepsiburadaMappingValidator>(
            mockContextFactory.Object,
            new Mock<ILogger<HepsiburadaMappingValidator>>().Object);

        _mockActivityLogger
            .Setup(x => x.LogAsync(
                It.IsAny<Guid>(), It.IsAny<ProductActivityType>(), It.IsAny<string>(),
                It.IsAny<ProductActivityStatus>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    private HepsiburadaProductService CreateSut() => new(
        mockContextFactory.Object,
        _mockApiClient.Object,
        _mockMapper.Object,
        _mockValidator.Object,
        _mockActivityLogger.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    // ── Test 1: PublishProductAsync — validation fails ──────────────────────

    [Fact]
    public async Task PublishProductAsync_ValidationFails_ReturnsError()
    {
        var productId = Guid.NewGuid();

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new ErrorResult("Kategori eşleşmesi yok"));

        var sut = CreateSut();
        var result = await sut.PublishProductAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kategori eşleşmesi yok");
        _mockApiClient.Verify(x => x.PostMultipartJsonFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── Test 2: PublishProductAsync — mapping fails ─────────────────────────

    [Fact]
    public async Task PublishProductAsync_MappingFails_ReturnsError()
    {
        var productId = Guid.NewGuid();

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        _mockMapper
            .Setup(x => x.MapProductAsync(productId))
            .ReturnsAsync(new ErrorDataResult<List<HepsiburadaProductItem>>(null, "Varyant yok"));

        var sut = CreateSut();
        var result = await sut.PublishProductAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Varyant yok");
    }

    // ── Test 3: PublishProductAsync — success returns trackingId ─────────────

    [Fact]
    public async Task PublishProductAsync_Success_ReturnsTrackingId()
    {
        var productId = Guid.NewGuid();
        var trackingId = "track-abc-123";

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        _mockMapper
            .Setup(x => x.MapProductAsync(productId))
            .ReturnsAsync(new SuccessDataResult<List<HepsiburadaProductItem>>(
                new List<HepsiburadaProductItem>
                {
                    new(123, "merchant-uuid", new Dictionary<string, object> { ["merchantSku"] = "TEST-SKU" })
                }));

        var trackingResponse = new HepsiburadaTrackingResponse(true, 200, null, new HepsiburadaTrackingData(trackingId));
        _mockApiClient
            .Setup(x => x.PostMultipartJsonFileAsync(
                It.Is<string>(u => u.Contains("/api/products/import")),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(trackingResponse));

        mockIntegrationDbContext.Setup(x => x.ProductMarketplaces)
            .ReturnsDbSet(new List<ProductMarketplace>());

        var sut = CreateSut();
        var result = await sut.PublishProductAsync(productId);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(trackingId);
    }

    // ── Test 4: PublishProductAsync — API error returns error ────────────────

    [Fact]
    public async Task PublishProductAsync_ApiError_ReturnsError()
    {
        var productId = Guid.NewGuid();

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        _mockMapper
            .Setup(x => x.MapProductAsync(productId))
            .ReturnsAsync(new SuccessDataResult<List<HepsiburadaProductItem>>(
                new List<HepsiburadaProductItem>()));

        _mockApiClient
            .Setup(x => x.PostMultipartJsonFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            });

        var sut = CreateSut();
        var result = await sut.PublishProductAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("API hatası");
    }

    // ── Test 5: CheckProductStatusAsync — success ───────────────────────────

    [Fact]
    public async Task CheckProductStatusAsync_Success_ReturnsStatusItems()
    {
        var trackingId = "track-xyz";
        var statusResponse = new HepsiburadaProductStatusResponse(
            true, 200, null,
            new HepsiburadaProductStatusData(1, new List<HepsiburadaProductStatusItem>
            {
                new("TEST-SKU", "HB-SKU-1", "1234567890123", "CREATED", "Test Urun",
                    "VG-001", "SUCCESS", null, null, null)
            }));

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains($"/api/products/status/{trackingId}"))))
            .ReturnsAsync(CreateJsonResponse(statusResponse));

        var sut = CreateSut();
        var result = await sut.CheckProductStatusAsync(trackingId);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].ProductStatus.Should().Be("CREATED");
        result.Data[0].MerchantSku.Should().Be("TEST-SKU");
    }

    // ── Test 6: CheckProductStatusAsync — API error ─────────────────────────

    [Fact]
    public async Task CheckProductStatusAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server Error")
            });

        var sut = CreateSut();
        var result = await sut.CheckProductStatusAsync("invalid-id");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Status API hatası");
    }

    // ── Test 7: ApprovePreMatchAsync — success ──────────────────────────────

    [Fact]
    public async Task ApprovePreMatchAsync_Success_ReturnsSuccess()
    {
        var marketplace = new Entity.MarketPlace
        {
            Id = MarketPlaceConstants.HepsiburadaMarketPlaceId,
            SellerId = "merchant-123"
        };

        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<Entity.MarketPlace> { marketplace });

        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/api/products/approve-prematch")),
                It.IsAny<HepsiburadaPreMatchApprovalRequest>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            });

        var sut = CreateSut();
        var result = await sut.ApprovePreMatchAsync("TEST-SKU");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("onaylandı");
    }

    // ── Test 8: ApprovePreMatchAsync — API error ────────────────────────────

    [Fact]
    public async Task ApprovePreMatchAsync_ApiError_ReturnsError()
    {
        var marketplace = new Entity.MarketPlace
        {
            Id = MarketPlaceConstants.HepsiburadaMarketPlaceId,
            SellerId = "merchant-123"
        };

        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<Entity.MarketPlace> { marketplace });

        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<HepsiburadaPreMatchApprovalRequest>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            });

        var sut = CreateSut();
        var result = await sut.ApprovePreMatchAsync("INVALID-SKU");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Onay hatası");
    }

    // ── Test 9: PublishProductAsync — exception handling ─────────────────────

    [Fact]
    public async Task PublishProductAsync_Exception_ReturnsError()
    {
        var productId = Guid.NewGuid();

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        _mockMapper
            .Setup(x => x.MapProductAsync(productId))
            .ReturnsAsync(new SuccessDataResult<List<HepsiburadaProductItem>>(
                new List<HepsiburadaProductItem>()));

        _mockApiClient
            .Setup(x => x.PostMultipartJsonFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();
        var result = await sut.PublishProductAsync(productId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    // ── Test 10: PublishProductAsync — validation logged ─────────────────────

    [Fact]
    public async Task PublishProductAsync_ValidationSuccess_LogsActivity()
    {
        var productId = Guid.NewGuid();

        _mockValidator
            .Setup(x => x.ValidateProductMappingsAsync(productId))
            .ReturnsAsync(new SuccessResult());

        _mockMapper
            .Setup(x => x.MapProductAsync(productId))
            .ReturnsAsync(new ErrorDataResult<List<HepsiburadaProductItem>>(null, "Mapping hatasi"));

        var sut = CreateSut();
        await sut.PublishProductAsync(productId);

        _mockActivityLogger.Verify(x => x.LogAsync(
            productId,
            ProductActivityType.MappingValidated,
            It.Is<string>(s => s.Contains("başarılı")),
            ProductActivityStatus.Success,
            It.IsAny<string?>(),
            It.Is<string>(s => s == "Hepsiburada"),
            It.IsAny<string?>()), Times.Once);
    }
}
