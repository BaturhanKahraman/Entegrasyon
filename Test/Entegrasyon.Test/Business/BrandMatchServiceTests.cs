using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Matches;
using FluentAssertions;
using MapsterMapper;

namespace Entegrasyon.UnitTest.Business;

public class BrandMatchServiceTests : BaseTest
{
    private readonly BrandMatchService _sut;
    private readonly Mock<IMapper> _mockMapper = new();

    public BrandMatchServiceTests()
    {
        MockValidator = new Mock<IFluentValidator>();

        _sut = new BrandMatchService(
            mockContextFactory.Object,
            MockValidator.Object,
            mockApplicationLogger.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetBrandMappingsByBrandId_ShouldReturnMappedDtos()
    {
        // Arrange
        var brandId = 1;
        var matches = new List<BrandMarketPlaceMatch>
        {
            new() { ApplicationBrandId = brandId, MarketPlaceId = 1, MarketPlaceBrandId = 100 },
            new() { ApplicationBrandId = brandId, MarketPlaceId = 2, MarketPlaceBrandId = 200 },
            new() { ApplicationBrandId = 99, MarketPlaceId = 1, MarketPlaceBrandId = 300 }
        };

        mockIntegrationDbContext
            .Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(matches);

        var expectedDtos = new List<BrandMarketPlaceMatchDto>
        {
            new() { ApplicationBrandId = brandId, MarketPlaceId = 1, MarketPlaceBrandId = 100 },
            new() { ApplicationBrandId = brandId, MarketPlaceId = 2, MarketPlaceBrandId = 200 }
        };

        _mockMapper
            .Setup(m => m.Map<List<BrandMarketPlaceMatch>, List<BrandMarketPlaceMatchDto>>(
                It.Is<List<BrandMarketPlaceMatch>>(l => l.Count == 2 && l.All(x => x.ApplicationBrandId == brandId))))
            .Returns(expectedDtos);

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

        _mockMapper
            .Setup(m => m.Map<List<BrandMarketPlaceMatch>, List<BrandMarketPlaceMatchDto>>(
                It.Is<List<BrandMarketPlaceMatch>>(l => l.Count == 0)))
            .Returns(new List<BrandMarketPlaceMatchDto>());

        // Act
        var result = await _sut.GetBrandMappingsByBrandIdAsync(999);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
