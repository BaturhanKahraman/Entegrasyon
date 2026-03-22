using System.Net;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

/// <summary>
/// PazaramaStockPriceService ve MockPazaramaStockPriceService için birim testleri.
/// Stock ve price güncellemelerinin ayrı endpoint'lere doğru gönderildiğini doğrular.
/// </summary>
public class PazaramaStockPriceServiceTests
{
    // -----------------------------------------------------------------------
    // Mock service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<ILogger<MockPazaramaStockPriceService>> _mockLoggerMock = new();

    private MockPazaramaStockPriceService CreateMockSut() =>
        new(_mockLoggerMock.Object);

    [Fact]
    public async Task MockService_UpdateStockAsync_Should_Return_Success_With_DataId()
    {
        var sut = CreateMockSut();
        var items = new List<PazaramaStockUpdateItem>
        {
            new("SKU-001", 10),
            new("SKU-002", 25)
        };

        var result = await sut.UpdateStockAsync(items);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Should().StartWith("mock-pazarama-stock-");
    }

    [Fact]
    public async Task MockService_UpdatePriceAsync_Should_Return_Success_With_DataId()
    {
        var sut = CreateMockSut();
        var items = new List<PazaramaPriceUpdateItem>
        {
            new("SKU-001", 100m, 89.99m),
            new("SKU-002", 200m, 179.99m)
        };

        var result = await sut.UpdatePriceAsync(items);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Should().StartWith("mock-pazarama-price-");
    }

    [Fact]
    public async Task MockService_Should_Return_Different_IDs_For_Different_Calls()
    {
        var sut = CreateMockSut();
        var stockItems = new List<PazaramaStockUpdateItem> { new("SKU-001", 10) };
        var priceItems = new List<PazaramaPriceUpdateItem> { new("SKU-001", 100m, 89.99m) };

        var stockResult = await sut.UpdateStockAsync(stockItems);
        var priceResult = await sut.UpdatePriceAsync(priceItems);
        var stockResult2 = await sut.UpdateStockAsync(stockItems);

        stockResult.Data.Should().NotBe(priceResult.Data);
        stockResult.Data.Should().NotBe(stockResult2.Data);
        priceResult.Data.Should().NotBe(stockResult2.Data);
    }

    // -----------------------------------------------------------------------
    // Real service testleri
    // -----------------------------------------------------------------------

    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaStockPriceService>> _loggerMock = new();

    private PazaramaStockPriceService CreateRealSut() => new(
        _apiClientMock.Object,
        _loggerMock.Object);

    private static HttpResponseMessage BuildSuccessStockPriceResponse(string dataId = "test-data-id-001")
    {
        var body = JsonSerializer.Serialize(new
        {
            data = dataId,
            success = true
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildErrorHttpResponse(HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent("{\"success\":false,\"message\":\"hata\"}", Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage BuildApiFailureResponse(string message = "İşlem başarısız")
    {
        var body = JsonSerializer.Serialize(new
        {
            data = (string?)null,
            success = false,
            message
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task UpdateStockAsync_WhenApiSucceeds_ShouldReturnDataId()
    {
        const string expectedDataId = "stock-data-id-xyz";
        var items = new List<PazaramaStockUpdateItem>
        {
            new("SKU-001", 10),
            new("SKU-002", 5)
        };

        _apiClientMock
            .Setup(a => a.PostAsync("product/updateStock-v2", It.IsAny<PazaramaStockUpdateRequest>()))
            .ReturnsAsync(BuildSuccessStockPriceResponse(expectedDataId));

        var sut = CreateRealSut();
        var result = await sut.UpdateStockAsync(items);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(expectedDataId);
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldCallCorrectEndpoint()
    {
        var items = new List<PazaramaStockUpdateItem> { new("SKU-001", 10) };

        _apiClientMock
            .Setup(a => a.PostAsync("product/updateStock-v2", It.IsAny<PazaramaStockUpdateRequest>()))
            .ReturnsAsync(BuildSuccessStockPriceResponse());

        var sut = CreateRealSut();
        await sut.UpdateStockAsync(items);

        _apiClientMock.Verify(
            a => a.PostAsync("product/updateStock-v2", It.IsAny<PazaramaStockUpdateRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePriceAsync_WhenApiSucceeds_ShouldReturnDataId()
    {
        const string expectedDataId = "price-data-id-xyz";
        var items = new List<PazaramaPriceUpdateItem>
        {
            new("SKU-001", 100m, 89.99m),
            new("SKU-002", 200m, 179.99m)
        };

        _apiClientMock
            .Setup(a => a.PostAsync("product/updatePrice-v2", It.IsAny<PazaramaPriceUpdateRequest>()))
            .ReturnsAsync(BuildSuccessStockPriceResponse(expectedDataId));

        var sut = CreateRealSut();
        var result = await sut.UpdatePriceAsync(items);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(expectedDataId);
    }

    [Fact]
    public async Task UpdatePriceAsync_ShouldCallCorrectEndpoint()
    {
        var items = new List<PazaramaPriceUpdateItem> { new("SKU-001", 100m, 89.99m) };

        _apiClientMock
            .Setup(a => a.PostAsync("product/updatePrice-v2", It.IsAny<PazaramaPriceUpdateRequest>()))
            .ReturnsAsync(BuildSuccessStockPriceResponse());

        var sut = CreateRealSut();
        await sut.UpdatePriceAsync(items);

        _apiClientMock.Verify(
            a => a.PostAsync("product/updatePrice-v2", It.IsAny<PazaramaPriceUpdateRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStockAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        var items = new List<PazaramaStockUpdateItem> { new("SKU-001", 10) };

        _apiClientMock
            .Setup(a => a.PostAsync("product/updateStock-v2", It.IsAny<PazaramaStockUpdateRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.InternalServerError));

        var sut = CreateRealSut();
        var result = await sut.UpdateStockAsync(items);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePriceAsync_WhenApiReturnsHttpError_ShouldReturnError()
    {
        var items = new List<PazaramaPriceUpdateItem> { new("SKU-001", 100m, 89.99m) };

        _apiClientMock
            .Setup(a => a.PostAsync("product/updatePrice-v2", It.IsAny<PazaramaPriceUpdateRequest>()))
            .ReturnsAsync(BuildErrorHttpResponse(HttpStatusCode.ServiceUnavailable));

        var sut = CreateRealSut();
        var result = await sut.UpdatePriceAsync(items);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStockAsync_WhenApiReturnsSuccessFalse_ShouldReturnError()
    {
        var items = new List<PazaramaStockUpdateItem> { new("SKU-001", 10) };

        _apiClientMock
            .Setup(a => a.PostAsync("product/updateStock-v2", It.IsAny<PazaramaStockUpdateRequest>()))
            .ReturnsAsync(BuildApiFailureResponse("Stok güncellenemedi"));

        var sut = CreateRealSut();
        var result = await sut.UpdateStockAsync(items);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePriceAsync_WhenApiReturnsSuccessFalse_ShouldReturnError()
    {
        var items = new List<PazaramaPriceUpdateItem> { new("SKU-001", 100m, 89.99m) };

        _apiClientMock
            .Setup(a => a.PostAsync("product/updatePrice-v2", It.IsAny<PazaramaPriceUpdateRequest>()))
            .ReturnsAsync(BuildApiFailureResponse("Fiyat güncellenemedi"));

        var sut = CreateRealSut();
        var result = await sut.UpdatePriceAsync(items);

        result.Success.Should().BeFalse();
    }
}
