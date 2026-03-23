using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Hepsiburada;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Test.Hepsiburada;

/// <summary>
/// HepsiburadaListingService unit testleri.
/// Fiyat/stok/kargo guncelleme, listing sorgulama, activate/deactivate islemleri test edilir.
/// </summary>
public class HepsiburadaListingServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHepsiburadaApiClient> _mockApiClient = new();
    private readonly Mock<ILogger<HepsiburadaListingService>> _mockLogger = new();

    public HepsiburadaListingServiceTests()
    {
        var marketplace = new Entity.MarketPlace
        {
            Id = MarketPlaceConstants.HepsiburadaMarketPlaceId,
            SellerId = "test-merchant-id"
        };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<Entity.MarketPlace> { marketplace });
    }

    private HepsiburadaListingService CreateSut() => new(
        mockContextFactory.Object,
        _mockApiClient.Object,
        _mockLogger.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK) { Content = new StringContent("", System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ErrorResponse() =>
        new(HttpStatusCode.BadRequest) { Content = new StringContent("Bad Request", System.Text.Encoding.UTF8, "application/json") };

    // ── Test 1: GetListingsAsync — success ──────────────────────────────────

    [Fact]
    public async Task GetListingsAsync_Success_ReturnsListings()
    {
        var listingResponse = new HepsiburadaListingResponse(
            new List<HepsiburadaListingItem>
            {
                new("HB-SKU-1", "MERCH-1", "Test Urun", 99.90m, 50, 3, "Yurtici", true, false, null)
            }, 1, 100, 0);

        _mockApiClient
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("/listings/merchantid/"))))
            .ReturnsAsync(CreateJsonResponse(listingResponse));

        var sut = CreateSut();
        var result = await sut.GetListingsAsync();

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Listings.Should().HaveCount(1);
        result.Data.Listings![0].HepsiburadaSku.Should().Be("HB-SKU-1");
    }

    // ── Test 2: GetListingsAsync — API error ────────────────────────────────

    [Fact]
    public async Task GetListingsAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.GetListingsAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Listing API hatası");
    }

    // ── Test 3: UpdatePricesAsync — success ─────────────────────────────────

    [Fact]
    public async Task UpdatePricesAsync_Success_ReturnsSuccess()
    {
        var updateResponse = new HepsiburadaListingUpdateResponse("update-1", "Completed", null);

        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("price-uploads")),
                It.IsAny<List<HepsiburadaPriceUpdateItem>>()))
            .ReturnsAsync(CreateJsonResponse(updateResponse));

        var items = new List<HepsiburadaPriceUpdateItem>
        {
            new("HB-SKU-1", "MERCH-1", 149.90m)
        };

        var sut = CreateSut();
        var result = await sut.UpdatePricesAsync(items);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("1 fiyat güncellendi");
    }

    // ── Test 4: UpdatePricesAsync — partial errors ──────────────────────────

    [Fact]
    public async Task UpdatePricesAsync_PartialErrors_ReturnsError()
    {
        var updateResponse = new HepsiburadaListingUpdateResponse("update-1", "PartialFailed",
            new List<HepsiburadaListingUpdateError>
            {
                new(0, "HB-SKU-BAD", "MERCH-BAD", new List<string> { "Gecersiz SKU" })
            });

        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<HepsiburadaPriceUpdateItem>>()))
            .ReturnsAsync(CreateJsonResponse(updateResponse));

        var items = new List<HepsiburadaPriceUpdateItem> { new("HB-SKU-BAD", "MERCH-BAD", 99m) };

        var sut = CreateSut();
        var result = await sut.UpdatePricesAsync(items);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Gecersiz SKU");
    }

    // ── Test 5: UpdateStocksAsync — success ─────────────────────────────────

    [Fact]
    public async Task UpdateStocksAsync_Success_ReturnsSuccess()
    {
        var updateResponse = new HepsiburadaListingUpdateResponse("update-2", "Completed", null);

        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("stock-uploads")),
                It.IsAny<List<HepsiburadaStockUpdateItem>>()))
            .ReturnsAsync(CreateJsonResponse(updateResponse));

        var items = new List<HepsiburadaStockUpdateItem>
        {
            new("HB-SKU-1", "MERCH-1", 100, null)
        };

        var sut = CreateSut();
        var result = await sut.UpdateStocksAsync(items);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("1 stok güncellendi");
    }

    // ── Test 6: UpdateStocksAsync — API error ───────────────────────────────

    [Fact]
    public async Task UpdateStocksAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<HepsiburadaStockUpdateItem>>()))
            .ReturnsAsync(ErrorResponse());

        var items = new List<HepsiburadaStockUpdateItem> { new("HB-1", "M-1", 10, null) };

        var sut = CreateSut();
        var result = await sut.UpdateStocksAsync(items);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Stok güncelleme hatası");
    }

    // ── Test 7: UpdateShippingInfoAsync — success ───────────────────────────

    [Fact]
    public async Task UpdateShippingInfoAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("shipping-info-uploads")),
                It.IsAny<List<HepsiburadaShippingInfoUpdateItem>>()))
            .ReturnsAsync(OkResponse());

        var items = new List<HepsiburadaShippingInfoUpdateItem>
        {
            new("HB-SKU-1", "MERCH-1", 3, "Yurtici", null, null, null, null)
        };

        var sut = CreateSut();
        var result = await sut.UpdateShippingInfoAsync(items);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("1 teslimat bilgisi güncellendi");
    }

    // ── Test 8: ActivateListingAsync — success ──────────────────────────────

    [Fact]
    public async Task ActivateListingAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/activate")),
                It.IsAny<object>()))
            .ReturnsAsync(OkResponse());

        var sut = CreateSut();
        var result = await sut.ActivateListingAsync("HB-SKU-1", "MERCH-1");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("satışa açıldı");
    }

    // ── Test 9: DeactivateListingAsync — success ────────────────────────────

    [Fact]
    public async Task DeactivateListingAsync_Success_ReturnsSuccess()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(
                It.Is<string>(u => u.Contains("/deactivate")),
                It.IsAny<object>()))
            .ReturnsAsync(OkResponse());

        var sut = CreateSut();
        var result = await sut.DeactivateListingAsync("HB-SKU-1", "MERCH-1");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("satıştan kapatıldı");
    }

    // ── Test 10: ActivateListingAsync — API error ───────────────────────────

    [Fact]
    public async Task ActivateListingAsync_ApiError_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(ErrorResponse());

        var sut = CreateSut();
        var result = await sut.ActivateListingAsync("HB-SKU-BAD", "MERCH-BAD");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Listing activate hatası");
    }

    // ── Test 11: UpdatePricesAsync — exception handling ─────────────────────

    [Fact]
    public async Task UpdatePricesAsync_Exception_ReturnsError()
    {
        _mockApiClient
            .Setup(x => x.PostAsync(It.IsAny<string>(), It.IsAny<List<HepsiburadaPriceUpdateItem>>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var items = new List<HepsiburadaPriceUpdateItem> { new("HB-1", "M-1", 99m) };

        var sut = CreateSut();
        var result = await sut.UpdatePricesAsync(items);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Network error");
    }
}
