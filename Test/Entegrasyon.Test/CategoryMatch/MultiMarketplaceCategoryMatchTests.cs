using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Business.Validation.FluentValidation;
using FluentValidation.Results;

namespace Entegrasyon.UnitTest.CategoryMatch;

public class MultiMarketplaceCategoryMatchTests : BaseTest
{
    private readonly CategoryMatchService _sut;

    public MultiMarketplaceCategoryMatchTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.Validate(It.IsAny<BulkCategoryMatchDto>()))
            .ReturnsAsync(new ValidationResult());

        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(new List<Category>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryMatchTemplates)
            .ReturnsDbSet(new List<CategoryMatchTemplate>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryMatchTemplateItems)
            .ReturnsDbSet(new List<CategoryMatchTemplateItem>());
        mockIntegrationDbContext
            .Setup(x => x.MarketPlaces)
            .ReturnsDbSet(new List<Entity.MarketPlace>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new CategoryMatchService(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object);
    }

    [Fact]
    public async Task GetCategoryMatchSummaryAsync_Parametric_FiltersByMarketplace()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" },
            new() { Id = 3, Name = "Kozmetik" }
        };
        var mappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true },
            new() { CategoryId = 2, MarketPlaceId = 1, IsActive = true },
            new() { CategoryId = 1, MarketPlaceId = 2, IsActive = true }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(mappings);

        // Act
        var result = await _sut.GetCategoryMatchSummaryAsync(1);

        // Assert
        result.TotalCategories.Should().Be(3);
        result.MappedCategories.Should().Be(2); // Only MarketPlaceId=1
        result.UnmappedCategories.Should().Be(1);
    }

    [Fact]
    public async Task GetCategoryMatchSummaryAsync_NoMarketPlaceId_ReturnsAllMapped()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" }
        };
        var mappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true },
            new() { CategoryId = 2, MarketPlaceId = 2, IsActive = true }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(mappings);

        // Act - no marketplace filter, returns overall unique mapped
        var result = await _sut.GetCategoryMatchSummaryAsync();

        // Assert
        result.TotalCategories.Should().Be(2);
        result.MappedCategories.Should().Be(2);
    }

    [Fact]
    public async Task GetCategoryMatchSummaryAsync_WithPerMarketplace_ReturnsBreakdown()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" },
            new() { Id = 3, Name = "Kozmetik" }
        };
        var mappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true },
            new() { CategoryId = 2, MarketPlaceId = 1, IsActive = true },
            new() { CategoryId = 1, MarketPlaceId = 2, IsActive = true }
        };
        var marketplaces = new List<Entity.MarketPlace>
        {
            new() { Id = 1, Name = "Trendyol" },
            new() { Id = 2, Name = "N11" }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(mappings);
        mockIntegrationDbContext.Setup(x => x.MarketPlaces).ReturnsDbSet(marketplaces);

        // Act
        var result = await _sut.GetCategoryMatchSummaryAsync();

        // Assert
        result.PerMarketplace.Should().NotBeNull();
        result.PerMarketplace.Should().HaveCount(2);

        var trendyol = result.PerMarketplace.First(m => m.MarketPlaceId == 1);
        trendyol.MappedCount.Should().Be(2);
        trendyol.MarketPlaceName.Should().Be("Trendyol");

        var n11 = result.PerMarketplace.First(m => m.MarketPlaceId == 2);
        n11.MappedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCategoryMatchSummaryAsync_Parametric_DifferentMarketplace()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" }
        };
        var mappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 2, IsActive = true }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(mappings);

        // Act
        var result = await _sut.GetCategoryMatchSummaryAsync(2);

        // Assert
        result.MappedCategories.Should().Be(1);
        result.UnmappedCategories.Should().Be(1);
    }
}
