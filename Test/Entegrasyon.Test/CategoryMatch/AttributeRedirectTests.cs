using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Matches;

namespace Entegrasyon.UnitTest.CategoryMatch;

/// <summary>
/// Tests for attribute redirect functionality:
/// CategoryMatchValidationService should report which categories lack attribute mappings
/// so the UI can show warning icons and redirect users to attribute sync.
/// </summary>
public class AttributeRedirectTests : BaseTest
{
    private readonly CategoryMatchValidationService _sut;

    public AttributeRedirectTests()
    {
        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(new List<Category>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeCategories)
            .ReturnsDbSet(new List<CategoryAttributeCategory>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryAttributeMarketPlaceMatches)
            .ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());
        mockIntegrationDbContext
            .Setup(x => x.BrandMarketPlaceMatches)
            .ReturnsDbSet(new List<BrandMarketPlaceMatch>());

        _sut = new CategoryMatchValidationService(mockContextFactory.Object);
    }

    [Fact]
    public async Task ValidateCategoryMatchAsync_WithMappingButNoAttributes_IsValidTrue()
    {
        // Arrange - category is mapped but has no required attributes at all
        var categories = new List<Category> { new() { Id = 1, Name = "Basit Kategori" } };
        var marketplaceMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceMappings);

        // Act
        var result = await _sut.ValidateCategoryMatchAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.IsValid.Should().BeTrue();
        result.Data.HasCategoryMapping.Should().BeTrue();
        result.Data.TotalRequiredAttributes.Should().Be(0);
    }

    [Fact]
    public async Task ValidateCategoryMatchAsync_MultipleUnmappedRequired_ReportsAllErrors()
    {
        // Arrange
        var categories = new List<Category> { new() { Id = 1, Name = "Detayli Kategori" } };
        var marketplaceMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true }
        };
        var attrCategories = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = 1, CategoryAttributeId = 10, IsRequired = true, CategoryAttribute = new CategoryAttribute { Id = 10, CategoryAttributeHumanized = "Renk" } },
            new() { CategoryId = 1, CategoryAttributeId = 11, IsRequired = true, CategoryAttribute = new CategoryAttribute { Id = 11, CategoryAttributeHumanized = "Beden" } },
            new() { CategoryId = 1, CategoryAttributeId = 12, IsRequired = true, CategoryAttribute = new CategoryAttribute { Id = 12, CategoryAttributeHumanized = "Materyal" } }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceMappings);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);
        // No attribute matches at all

        // Act
        var result = await _sut.ValidateCategoryMatchAsync(1, 1);

        // Assert
        result.Data.IsValid.Should().BeFalse();
        result.Data.TotalRequiredAttributes.Should().Be(3);
        result.Data.MappedRequiredAttributes.Should().Be(0);
        result.Data.Errors.Should().HaveCount(3);
        result.Data.Errors.Should().Contain(e => e.Contains("Renk"));
        result.Data.Errors.Should().Contain(e => e.Contains("Beden"));
        result.Data.Errors.Should().Contain(e => e.Contains("Materyal"));
    }

    [Fact]
    public async Task ValidateAllMatchesAsync_MixedCategories_ReturnsCorrectCounts()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Tamam" },
            new() { Id = 2, Name = "Eksik" }
        };
        var marketplaceMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true },
            new() { CategoryId = 2, MarketPlaceId = 1, IsActive = true }
        };
        var attrCategories = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = 2, CategoryAttributeId = 20, IsRequired = true, CategoryAttribute = new CategoryAttribute { Id = 20, CategoryAttributeHumanized = "Renk" } }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceMappings);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);

        // Act
        var result = await _sut.ValidateAllMatchesAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        var validCount = result.Data.Count(v => v.IsValid);
        var invalidCount = result.Data.Count(v => !v.IsValid);
        validCount.Should().Be(1);
        invalidCount.Should().Be(1);
    }
}
