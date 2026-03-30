using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Matches;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Business;

public class BrandMatchServiceTests : BaseTest
{
    private readonly BrandMatchService _sut;
    private readonly BrandMatchMapper _brandMatchMapper = new();

    public BrandMatchServiceTests()
    {
        MockValidator = new Mock<IFluentValidator>();

        _sut = new BrandMatchService(
            mockContextFactory.Object,
            MockValidator.Object,
            mockApplicationLogger.Object,
            _brandMatchMapper);
    }

    [Fact]
    public async Task GetBrandMappingsByBrandId_ShouldReturnMappedDtos()
    {
        // Arrange
        var brandId = 1;
        var brand = new Brand { Id = brandId, Name = "Test Brand" };
        var matches = new List<BrandMarketPlaceMatch>
        {
            new() { ApplicationBrandId = brandId, MarketPlaceId = 1, MarketPlaceBrandId = 100, ApplicationBrand = brand },
            new() { ApplicationBrandId = brandId, MarketPlaceId = 2, MarketPlaceBrandId = 200, ApplicationBrand = brand },
            new() { ApplicationBrandId = 99, MarketPlaceId = 1, MarketPlaceBrandId = 300, ApplicationBrand = new Brand { Id = 99, Name = "Other" } }
        };

        mockIntegrationDbContext
            .Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(matches);

        // Act
        var result = await _sut.GetBrandMappingsByBrandIdAsync(brandId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().Contain(d => d.MarketPlaceBrandId == 100);
        result.Data.Should().Contain(d => d.MarketPlaceBrandId == 200);
    }

    [Fact]
    public async Task GetBrandMappingsByBrandId_WhenNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>());

        // Act
        var result = await _sut.GetBrandMappingsByBrandIdAsync(999);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
