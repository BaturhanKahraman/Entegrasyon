using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Matches;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

public class PazaramaBrandServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaBrandService>> _loggerMock = new();

    private PazaramaBrandService CreateSut() => new(
        _apiClientMock.Object,
        mockContextFactory.Object,
        _loggerMock.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static PazaramaResponse<List<PazaramaBrandDto>> WrapBrands(List<PazaramaBrandDto> brands) =>
        new(Data: brands, Success: true, MessageCode: null, Message: null, UserMessage: null, FromCache: false);

    // -----------------------------------------------------------------------
    // GetBrandsAsync — success scenarios
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetBrandsAsync_ShouldReturnBrands()
    {
        // Arrange
        var brands = new List<PazaramaBrandDto>
        {
            new(Guid.NewGuid(), "Apple",   "https://logo.url/apple.png",  "apple.com",   true,  "apple"),
            new(Guid.NewGuid(), "Samsung", "https://logo.url/samsung.png", "samsung.com", true, "samsung"),
        };

        _apiClientMock
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("getBrands"))))
            .ReturnsAsync(CreateJsonResponse(WrapBrands(brands)));

        var sut = CreateSut();

        // Act
        var result = await sut.GetBrandsAsync();

        // Assert
        result.Success.Should().BeTrue();
        var data = result.Data!.ToList();
        data.Should().HaveCount(2);
        data[0].Name.Should().Be("Apple");
        data[1].Name.Should().Be("Samsung");
    }

    [Fact]
    public async Task GetBrandsAsync_WhenNameFilterProvided_ShouldAppendNameToUrl()
    {
        // Arrange
        var brands = new List<PazaramaBrandDto>
        {
            new(Guid.NewGuid(), "Apple", null, null, true, "apple"),
        };

        string? capturedUrl = null;
        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .Callback<string>(url => capturedUrl = url)
            .ReturnsAsync(CreateJsonResponse(WrapBrands(brands)));

        var sut = CreateSut();

        // Act
        var result = await sut.GetBrandsAsync(nameFilter: "Apple");

        // Assert
        result.Success.Should().BeTrue();
        capturedUrl.Should().Contain("name=Apple");
    }

    [Fact]
    public async Task GetBrandsAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(WrapBrands(new List<PazaramaBrandDto>())));

        var sut = CreateSut();

        // Act
        var result = await sut.GetBrandsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBrandsAsync_WhenApiFails_ShouldReturnError()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetBrandsAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    // -----------------------------------------------------------------------
    // ImportBrandsAsync — new brand + match creation
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportBrandsAsync_WhenBrandAndMatchDontExist_ShouldCreateBrandAndMatch()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var brands = new List<PazaramaBrandDto>
        {
            new(brandId, "NewBrand", null, null, true, "new-brand"),
        };

        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(WrapBrands(brands)));

        // No existing matches
        mockIntegrationDbContext
            .Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>());

        // No existing brand by name
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(new List<Brand>());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ImportBrandsAsync();

        // Assert
        result.Success.Should().BeTrue();
        mockIntegrationDbContext.Verify(
            x => x.BrandMarketPlaceMatches.Add(It.Is<BrandMarketPlaceMatch>(m =>
                m.MarketPlaceBrandExternalId == brandId.ToString() &&
                m.MarketPlaceBrandId == 0)),
            Times.Once);
    }

    [Fact]
    public async Task ImportBrandsAsync_WhenMatchAlreadyExists_ShouldSkipBrand()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var brands = new List<PazaramaBrandDto>
        {
            new(brandId, "ExistingBrand", null, null, true, "existing-brand"),
        };

        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(WrapBrands(brands)));

        // Match already exists for this brand GUID and marketplace
        var existingMatch = new BrandMarketPlaceMatch
        {
            ApplicationBrandId = 1,
            MarketPlaceId = 5,
            MarketPlaceBrandId = 0,
            MarketPlaceBrandExternalId = brandId.ToString()
        };
        mockIntegrationDbContext
            .Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch> { existingMatch });

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();

        // Act
        var result = await sut.ImportBrandsAsync();

        // Assert
        result.Success.Should().BeTrue();
        mockIntegrationDbContext.Verify(
            x => x.BrandMarketPlaceMatches.Add(It.IsAny<BrandMarketPlaceMatch>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportBrandsAsync_WhenBrandExistsByName_ShouldOnlyCreateMatch()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var brands = new List<PazaramaBrandDto>
        {
            new(brandId, "ExistingBrandName", null, null, true, "existing-brand-name"),
        };

        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(WrapBrands(brands)));

        // No existing match
        mockIntegrationDbContext
            .Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>());

        // Brand exists by name
        var existingBrand = new Brand { Id = 42, Name = "ExistingBrandName" };
        mockIntegrationDbContext
            .Setup(x => x.Brands)
            .ReturnsDbSet(new List<Brand> { existingBrand });

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ImportBrandsAsync();

        // Assert
        result.Success.Should().BeTrue();
        mockIntegrationDbContext.Verify(
            x => x.BrandMarketPlaceMatches.Add(It.Is<BrandMarketPlaceMatch>(m =>
                m.ApplicationBrandId == 42 &&
                m.MarketPlaceBrandExternalId == brandId.ToString())),
            Times.Once);
        // Brand itself should NOT be added
        mockIntegrationDbContext.Verify(
            x => x.Brands.Add(It.IsAny<Brand>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportBrandsAsync_WhenGetBrandsFails_ShouldReturnError()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API down"));

        var sut = CreateSut();

        // Act
        var result = await sut.ImportBrandsAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("API down");
    }
}
