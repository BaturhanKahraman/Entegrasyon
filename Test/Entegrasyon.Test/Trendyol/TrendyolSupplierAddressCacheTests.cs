using System.Net;
using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolSupplierAddressCache unit tests — verifies memory cache behavior,
/// API call, default address selection, and error handling.
/// </summary>
public class TrendyolSupplierAddressCacheTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITrendyolApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<TrendyolSupplierAddressCache>> _loggerMock = new();
    private readonly MemoryCache _realCache = new(new MemoryCacheOptions());

    private TrendyolSupplierAddressCache CreateSut() => new(
        mockContextFactory.Object,
        _apiClientMock.Object,
        _realCache,
        _loggerMock.Object);

    private void SetupMarketPlace(string? sellerId = "12345")
    {
        var mp = new MarketPlace { Id = TrendyolMarketPlaceId, Name = "Trendyol", SellerId = sellerId };
        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(new List<MarketPlace> { mp });
    }

    private static TrendyolAddressListResponse CreateAddressResponse(params TrendyolSupplierAddress[] addresses) =>
        new(addresses.ToList());

    public override string ToString() => nameof(TrendyolSupplierAddressCacheTests);

    // ── Test 1: Returns default address from API ──

    [Fact]
    public async Task GetDefaultAddressAsync_ReturnsDefaultAddress_FromApi()
    {
        // Arrange
        SetupMarketPlace("12345");

        var addressResponse = CreateAddressResponse(
            new TrendyolSupplierAddress(1, "Adres 1", "Istanbul", "Kadikoy", "34000", false, true, false, false),
            new TrendyolSupplierAddress(2, "Adres 2", "Ankara", "Cankaya", "06000", true, true, true, true));

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(addressResponse)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetDefaultAddressAsync();

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
        result.City.Should().Be("Ankara");
    }

    // ── Test 2: Returns first address when no default ──

    [Fact]
    public async Task GetDefaultAddressAsync_WhenNoDefault_ReturnsFirst()
    {
        // Arrange
        SetupMarketPlace("12345");

        var addressResponse = CreateAddressResponse(
            new TrendyolSupplierAddress(1, "Adres 1", "Istanbul", "Kadikoy", "34000", false, true, false, false),
            new TrendyolSupplierAddress(2, "Adres 2", "Izmir", "Bornova", "35000", false, true, true, true));

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(addressResponse)
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetDefaultAddressAsync();

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be("Istanbul");
    }

    // ── Test 3: Uses cached value on second call ──

    [Fact]
    public async Task GetDefaultAddressAsync_SecondCall_UsesCachedValue()
    {
        // Arrange
        SetupMarketPlace("12345");

        var addressResponse = CreateAddressResponse(
            new TrendyolSupplierAddress(1, "Cached Addr", "Istanbul", "Besiktas", "34000", true, true, true, true));

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(addressResponse)
            });

        var sut = CreateSut();

        // Act
        var firstResult = await sut.GetDefaultAddressAsync();
        var secondResult = await sut.GetDefaultAddressAsync();

        // Assert
        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        secondResult!.City.Should().Be("Istanbul");

        // API should be called only once — second call from cache
        _apiClientMock.Verify(c => c.GetAsync(It.IsAny<string>()), Times.Once);
    }

    // ── Test 4: SellerId missing returns null ──

    [Fact]
    public async Task GetDefaultAddressAsync_WhenSellerIdMissing_ReturnsNull()
    {
        // Arrange
        SetupMarketPlace(sellerId: null);
        var sut = CreateSut();

        // Act
        var result = await sut.GetDefaultAddressAsync();

        // Assert
        result.Should().BeNull();
    }

    // ── Test 5: API error returns null ──

    [Fact]
    public async Task GetDefaultAddressAsync_WhenApiReturnsError_ReturnsNull()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateSut();

        // Act
        var result = await sut.GetDefaultAddressAsync();

        // Assert
        result.Should().BeNull();
    }

    // ── Test 6: Exception returns null (graceful) ──

    [Fact]
    public async Task GetDefaultAddressAsync_WhenExceptionThrown_ReturnsNull()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetDefaultAddressAsync();

        // Assert
        result.Should().BeNull();
    }

    // ── Test 7: Empty address list returns null ──

    [Fact]
    public async Task GetDefaultAddressAsync_WhenEmptyAddressList_ReturnsNull()
    {
        // Arrange
        SetupMarketPlace("12345");

        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TrendyolAddressListResponse(new List<TrendyolSupplierAddress>()))
            });

        var sut = CreateSut();

        // Act
        var result = await sut.GetDefaultAddressAsync();

        // Assert
        result.Should().BeNull();
    }

    // ── Test 8: Uses correct URL with SellerId ──

    [Fact]
    public async Task GetDefaultAddressAsync_UsesCorrectUrl()
    {
        // Arrange
        SetupMarketPlace("99999");

        string? capturedUrl = null;
        _apiClientMock
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .Callback<string>(url => capturedUrl = url)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TrendyolAddressListResponse(null))
            });

        var sut = CreateSut();

        // Act
        await sut.GetDefaultAddressAsync();

        // Assert
        capturedUrl.Should().Contain("99999");
        capturedUrl.Should().Contain("addresses");
    }
}
