using System.Net;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Trendyol;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolOrderService unit tests — verifies order fetch, unsupplied,
/// tracking update, and shipping label retrieval.
/// </summary>
public class TrendyolOrderServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITrendyolApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<TrendyolOrderService>> _loggerMock = new();

    private TrendyolOrderService CreateSut() => new(
        mockContextFactory.Object,
        _apiClientMock.Object,
        _loggerMock.Object);

    private void SetupMarketPlace(string? sellerId = "12345")
    {
        var mp = new MarketPlace { Id = TrendyolMarketPlaceId, Name = "Trendyol", SellerId = sellerId };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace> { mp });
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FetchOrdersAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FetchOrdersAsync_HappyPath_ReturnsOrders()
    {
        // Arrange
        SetupMarketPlace("12345");

        var orderList = new TrendyolOrderListResponse(0, 50, 1, 1,
        [
            new TrendyolShipmentPackage(1001, "ORD-001", "2024-01-01", "Created", 100m, 0, 100m, false, false, null, null, null, null, null, null)
        ]);

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(orderList)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.FetchOrdersAsync(new TrendyolOrderQueryParams());

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].OrderNumber.Should().Be("ORD-001");
    }

    [Fact]
    public async Task FetchOrdersAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.FetchOrdersAsync(new TrendyolOrderQueryParams());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SellerId");
    }

    [Fact]
    public async Task FetchOrdersAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.FetchOrdersAsync(new TrendyolOrderQueryParams());

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task FetchOrdersAsync_IncludesDateAndStatusInUrl()
    {
        // Arrange
        SetupMarketPlace("12345");

        string? capturedUrl = null;
        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .Callback<string>(url => capturedUrl = url)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TrendyolOrderListResponse(0, 50, 0, 0, null))
            });

        var query = new TrendyolOrderQueryParams(
            StartDate: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate: new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero),
            Status: "Created");

        var sut = CreateSut();

        // Act
        await sut.FetchOrdersAsync(query);

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl.Should().Contain("startDate=");
        capturedUrl.Should().Contain("endDate=");
        capturedUrl.Should().Contain("status=Created");
    }

    [Fact]
    public async Task FetchOrdersAsync_EmptyContent_ReturnsEmptyList()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TrendyolOrderListResponse(0, 50, 0, 0, null))
            });

        var sut = CreateSut();

        // Act
        var result = await sut.FetchOrdersAsync(new TrendyolOrderQueryParams());

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MarkUnsuppliedAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MarkUnsuppliedAsync_HappyPath_UsesItemsUnsuppliedUrlWithReasonId()
    {
        // Arrange
        SetupMarketPlace("12345");

        string? capturedUrl = null;
        object? capturedBody = null;
        _apiClientMock
            .Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((u, b) => { capturedUrl = u; capturedBody = b; })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        var result = await sut.MarkUnsuppliedAsync(1001, [1, 2, 3], reasonId: 500);

        // Assert — Trendyol cancelorderpackageitem: .../items/unsupplied + reasonId zorunlu.
        result.Success.Should().BeTrue();
        capturedUrl.Should().Contain("/shipment-packages/1001/items/unsupplied");

        var bodyJson = System.Text.Json.JsonSerializer.Serialize(
            capturedBody, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        bodyJson.Should().Contain("\"reasonId\":500");
        bodyJson.Should().Contain("lines");
    }

    [Fact]
    public async Task MarkUnsuppliedAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.MarkUnsuppliedAsync(1001, [1], reasonId: 500);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("SellerId");
    }

    [Fact]
    public async Task MarkUnsuppliedAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.MarkUnsuppliedAsync(1001, [1], reasonId: 500);

        // Assert
        result.Success.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UpdateTrackingNumberAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateTrackingNumberAsync_HappyPath_UsesUpdateTrackingNumberUrl()
    {
        // Arrange
        SetupMarketPlace("12345");

        string? capturedUrl = null;
        _apiClientMock
            .Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((u, _) => capturedUrl = u)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateTrackingNumberAsync(1001, "TRACK-12345");

        // Assert — "Update Shipping Code" endpoint'i .../update-tracking-number (Notify Packages değil).
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("takip");
        capturedUrl.Should().Contain("/shipment-packages/1001/update-tracking-number");
    }

    [Fact]
    public async Task UpdateTrackingNumberAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.UpdateTrackingNumberAsync(1001, "TRACK-12345");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTrackingNumberAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error")
            });

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateTrackingNumberAsync(1001, "TRACK-12345");

        // Assert
        result.Success.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetShippingLabelAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetShippingLabelAsync_HappyPath_ReturnsBytes()
    {
        // Arrange
        SetupMarketPlace("12345");

        var labelBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header
        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(labelBytes)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetShippingLabelAsync(1001);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(labelBytes);
    }

    [Fact]
    public async Task GetShippingLabelAsync_WhenSellerIdMissing_ReturnsError()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.GetShippingLabelAsync(1001);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetShippingLabelAsync_WhenApiReturnsError_ReturnsError()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var sut = CreateSut();

        // Act
        var result = await sut.GetShippingLabelAsync(1001);

        // Assert
        result.Success.Should().BeFalse();
    }
}
