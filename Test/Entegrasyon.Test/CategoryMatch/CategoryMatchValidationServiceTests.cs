using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Matches;

namespace Entegrasyon.UnitTest.CategoryMatch;

public class CategoryMatchValidationServiceTests : BaseTest
{
    private readonly CategoryMatchValidationService _sut;

    public CategoryMatchValidationServiceTests()
    {
        // Default empty collections
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
    public async Task ValidateCategoryMatchAsync_NoCategoryMapping_ReturnsInvalid()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" }
        };
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        // Act
        var result = await _sut.ValidateCategoryMatchAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.IsValid.Should().BeFalse();
        result.Data.HasCategoryMapping.Should().BeFalse();
        result.Data.Errors.Should().Contain(e => e.Contains("eşleştirmesi"));
    }

    [Fact]
    public async Task ValidateCategoryMatchAsync_AllRequiredAttributesMapped_ReturnsValid()
    {
        // Arrange
        var categories = new List<Category> { new() { Id = 1, Name = "Elektronik" } };
        var marketplaceMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true }
        };
        var attrCategories = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = 1, CategoryAttributeId = 10, IsRequired = true, CategoryAttribute = new CategoryAttribute { Id = 10, CategoryAttributeHumanized = "Renk" } },
            new() { CategoryId = 1, CategoryAttributeId = 11, IsRequired = false, CategoryAttribute = new CategoryAttribute { Id = 11, CategoryAttributeHumanized = "Materyal" } }
        };
        var attrMatches = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 10, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 500 }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceMappings);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(attrMatches);

        // Act
        var result = await _sut.ValidateCategoryMatchAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.IsValid.Should().BeTrue();
        result.Data.HasCategoryMapping.Should().BeTrue();
        result.Data.TotalRequiredAttributes.Should().Be(1);
        result.Data.MappedRequiredAttributes.Should().Be(1);
    }

    [Fact]
    public async Task ValidateCategoryMatchAsync_MissingRequiredAttribute_ReturnsInvalid()
    {
        // Arrange
        var categories = new List<Category> { new() { Id = 1, Name = "Elektronik" } };
        var marketplaceMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true }
        };
        var attrCategories = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = 1, CategoryAttributeId = 10, IsRequired = true, CategoryAttribute = new CategoryAttribute { Id = 10, CategoryAttributeHumanized = "Renk" } },
            new() { CategoryId = 1, CategoryAttributeId = 11, IsRequired = true, CategoryAttribute = new CategoryAttribute { Id = 11, CategoryAttributeHumanized = "Beden" } }
        };
        var attrMatches = new List<CategoryAttributeMarketPlaceMatch>
        {
            new() { ApplicationCategoryAttributeId = 10, MarketPlaceId = 1, MarketPlaceCategoryAttributeId = 500 }
            // attr 11 NOT mapped
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceMappings);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(attrMatches);

        // Act
        var result = await _sut.ValidateCategoryMatchAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.IsValid.Should().BeFalse();
        result.Data.TotalRequiredAttributes.Should().Be(2);
        result.Data.MappedRequiredAttributes.Should().Be(1);
        result.Data.Errors.Should().Contain(e => e.Contains("Beden"));
    }

    [Fact]
    public async Task ValidateCategoryMatchAsync_MissingVarianterAttribute_ReturnsWarning()
    {
        // Arrange
        var categories = new List<Category> { new() { Id = 1, Name = "Elektronik" } };
        var marketplaceMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true }
        };
        var attrCategories = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = 1, CategoryAttributeId = 10, IsRequired = false, IsVarianter = true, CategoryAttribute = new CategoryAttribute { Id = 10, CategoryAttributeHumanized = "Renk" } }
        };
        var attrMatches = new List<CategoryAttributeMarketPlaceMatch>();

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceMappings);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(attrMatches);

        // Act
        var result = await _sut.ValidateCategoryMatchAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalVarianterAttributes.Should().Be(1);
        result.Data.MappedVarianterAttributes.Should().Be(0);
        result.Data.Warnings.Should().Contain(w => w.Contains("Renk"));
    }

    [Fact]
    public async Task ValidateCategoryMatchAsync_CategoryNotFound_ReturnsError()
    {
        // Arrange - no categories in db

        // Act
        var result = await _sut.ValidateCategoryMatchAsync(999, 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task ValidateAllMatchesAsync_ReturnsResultForEachMappedCategory()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" }
        };
        var marketplaceMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true },
            new() { CategoryId = 2, MarketPlaceId = 1, IsActive = true }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceMappings);

        // Act
        var result = await _sut.ValidateAllMatchesAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateCategoryMatchAsync_RequiredVarianterAttribute_ReportsAsError()
    {
        // Arrange - a varianter attribute that is also required should appear in errors, not warnings
        var categories = new List<Category> { new() { Id = 1, Name = "Elektronik" } };
        var marketplaceMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true }
        };
        var attrCategories = new List<CategoryAttributeCategory>
        {
            new() { CategoryId = 1, CategoryAttributeId = 10, IsRequired = true, IsVarianter = true, CategoryAttribute = new CategoryAttribute { Id = 10, CategoryAttributeHumanized = "Renk" } }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceMappings);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(new List<CategoryAttributeMarketPlaceMatch>());

        // Act
        var result = await _sut.ValidateCategoryMatchAsync(1, 1);

        // Assert
        result.Data.IsValid.Should().BeFalse();
        result.Data.Errors.Should().Contain(e => e.Contains("Renk"));
        result.Data.TotalRequiredAttributes.Should().Be(1);
        result.Data.TotalVarianterAttributes.Should().Be(1);
    }
}
