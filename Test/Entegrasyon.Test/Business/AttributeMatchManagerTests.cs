using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Matches;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Business;

public class AttributeMatchManagerTests : BaseTest
{
    private readonly AttributeMatchManager _sut;

    public AttributeMatchManagerTests()
    {
        _sut = new AttributeMatchManager(mockContextFactory.Object);
    }

    // ──────────────────────────────────────────────────────────────────
    // GetAttributeMatchesAsync
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAttributeMatchesAsync_ShouldReturnMatchesFilteredByMarketPlaceId()
    {
        // Arrange
        var matches = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 100 },
            new() { ApplicationCategoryAttributeId = 2, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 200 },
            new() { ApplicationCategoryAttributeId = 3, MarketPlaceId = 2, MarketPlaceCategoryAttributeId = 300 },
        };

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(matches);

        // Act
        var result = await _sut.GetAttributeMatchesAsync(1, new List<int> { 1, 2, 3 });

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.MarketPlaceId == 1);
    }

    [Fact]
    public async Task GetAttributeMatchesAsync_ShouldFilterByAttributeIds()
    {
        // Arrange
        var matches = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 100 },
            new() { ApplicationCategoryAttributeId = 2, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 200 },
            new() { ApplicationCategoryAttributeId = 3, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 300 },
        };

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(matches);

        // Act
        var result = await _sut.GetAttributeMatchesAsync(1, new List<int> { 1, 2 });

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.ApplicationCategoryAttributeId == 1);
        result.Should().Contain(m => m.ApplicationCategoryAttributeId == 2);
        result.Should().NotContain(m => m.ApplicationCategoryAttributeId == 3);
    }

    // ──────────────────────────────────────────────────────────────────
    // SaveAttributeMatchAsync
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAttributeMatchAsync_ShouldSucceed_WhenMatchDoesNotExist()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        // Act
        var result = await _sut.SaveAttributeMatchAsync(1, 1, 100);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAttributeMatchAsync_ShouldReturnError_WhenMatchAlreadyExists()
    {
        // Arrange
        var existing = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 100 }
        };

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(existing);

        // Act
        var result = await _sut.SaveAttributeMatchAsync(1, 1, 200);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten eşleştirilmiş");
    }

    // ──────────────────────────────────────────────────────────────────
    // RemoveAttributeMatchAsync
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAttributeMatchAsync_ShouldSucceed_WhenMatchExists()
    {
        // Arrange
        var existing = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 1, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 100 }
        };

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(existing);

        // Act
        var result = await _sut.RemoveAttributeMatchAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveAttributeMatchAsync_ShouldReturnError_WhenMatchNotFound()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        // Act
        var result = await _sut.RemoveAttributeMatchAsync(99, 1);

        // Assert
        result.Success.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────
    // SaveValueMatchAsync
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveValueMatchAsync_ShouldSucceed_WhenValueMatchDoesNotExist()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());

        // Act
        var result = await _sut.SaveValueMatchAsync(10, 1, 1000);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SaveValueMatchAsync_ShouldReturnError_WhenValueMatchAlreadyExists()
    {
        // Arrange
        var existing = new List<CategoryAttributeValueMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeValueId = 10, MarketPlaceId = 1, MarketPlaceCategoryAttributeValueId = 1000 }
        };

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(existing);

        // Act
        var result = await _sut.SaveValueMatchAsync(10, 1, 2000);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten eşleştirilmiş");
    }

    // ──────────────────────────────────────────────────────────────────
    // RemoveValueMatchAsync
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveValueMatchAsync_ShouldSucceed_WhenValueMatchExists()
    {
        // Arrange
        var existing = new List<CategoryAttributeValueMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeValueId = 10, MarketPlaceId = 1, MarketPlaceCategoryAttributeValueId = 1000 }
        };

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(existing);

        // Act
        var result = await _sut.RemoveValueMatchAsync(10, 1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveValueMatchAsync_ShouldReturnError_WhenValueMatchNotFound()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeValueMarketPlaceMatch>());

        // Act
        var result = await _sut.RemoveValueMatchAsync(99, 1);

        // Assert
        result.Success.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────
    // GetValueMatchesAsync
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetValueMatchesAsync_ShouldReturnMatchesFilteredByMarketPlaceIdAndValueIds()
    {
        // Arrange
        var matches = new List<CategoryAttributeValueMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeValueId = 10, MarketPlaceId = 1, MarketPlaceCategoryAttributeValueId = 1000 },
            new() { ApplicationCategoryAttributeValueId = 20, MarketPlaceId = 1, MarketPlaceCategoryAttributeValueId = 2000 },
            new() { ApplicationCategoryAttributeValueId = 30, MarketPlaceId = 2, MarketPlaceCategoryAttributeValueId = 3000 },
        };

        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeValueMarketPlaceMatches)
            .ReturnsDbSet(matches);

        // Act
        var result = await _sut.GetValueMatchesAsync(1, new List<int> { 10, 20, 30 });

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.MarketPlaceId == 1);
    }
}
