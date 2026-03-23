using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolMarketplaceSearchService unit tests — verifies category search (with flattening),
/// brand search (stub), and attribute search.
/// </summary>
public class TrendyolMarketplaceSearchServiceTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITrendyolCategoryImportService> _categoryImportMock = new();
    private readonly Mock<ILogger<TrendyolMarketplaceSearchService>> _loggerMock = new();

    private TrendyolMarketplaceSearchService CreateSut() => new(
        mockContextFactory.Object,
        _categoryImportMock.Object,
        _loggerMock.Object);

    // ═══════════════════════════════════════════════════════════════════════
    // SearchCategoriesAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchCategoriesAsync_ReturnsMatchingCategories()
    {
        // Arrange
        var categories = new List<ImportedTrendyolCategory>
        {
            new() { Id = 1, Name = "Giyim", SubCategories = new List<ImportedTrendyolCategory>
            {
                new() { Id = 2, Name = "Erkek Giyim" },
                new() { Id = 3, Name = "Kadin Giyim" }
            }},
            new() { Id = 4, Name = "Elektronik" }
        };

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "Erkek");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("Erkek Giyim");
    }

    [Fact]
    public async Task SearchCategoriesAsync_EmptyQuery_ReturnsAllFlattened()
    {
        // Arrange
        var categories = new List<ImportedTrendyolCategory>
        {
            new() { Id = 1, Name = "Giyim", SubCategories = new List<ImportedTrendyolCategory>
            {
                new() { Id = 2, Name = "Erkek Giyim" }
            }},
            new() { Id = 3, Name = "Elektronik" }
        };

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(3); // Giyim, Erkek Giyim, Elektronik
    }

    [Fact]
    public async Task SearchCategoriesAsync_LimitsResultsTo20()
    {
        // Arrange
        var categories = Enumerable.Range(1, 30)
            .Select(i => new ImportedTrendyolCategory { Id = i, Name = $"Category {i}" })
            .ToList();

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(20);
    }

    [Fact]
    public async Task SearchCategoriesAsync_WhenImportServiceFails_ReturnsError()
    {
        // Arrange
        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new ErrorDataResult<IEnumerable<ImportedTrendyolCategory>>([], "API hatasi"));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "test");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SearchCategoriesAsync_WhenExceptionThrown_ReturnsError()
    {
        // Arrange
        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "test");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("hata");
    }

    [Fact]
    public async Task SearchCategoriesAsync_FullPathIncludesParent()
    {
        // Arrange
        var categories = new List<ImportedTrendyolCategory>
        {
            new() { Id = 1, Name = "Giyim", SubCategories = new List<ImportedTrendyolCategory>
            {
                new() { Id = 2, Name = "Erkek", SubCategories = new List<ImportedTrendyolCategory>
                {
                    new() { Id = 3, Name = "Tisort" }
                }}
            }}
        };

        _categoryImportMock
            .Setup(s => s.GetTrendyolCategories())
            .ReturnsAsync(new SuccessDataResult<IEnumerable<ImportedTrendyolCategory>>(categories));

        var sut = CreateSut();

        // Act
        var result = await sut.SearchCategoriesAsync(1, "Tisort");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].FullPath.Should().Be("Giyim > Erkek > Tisort");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchBrandsAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchBrandsAsync_ReturnsEmptyList_NotYetImplemented()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.SearchBrandsAsync(1, "Nike");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SearchAttributesAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SearchAttributesAsync_ReturnsMatchingAttributes()
    {
        // Arrange
        var attrs = new List<CategoryAttribute>
        {
            new() { Id = 1, CategoryAttributeKey = "color", CategoryAttributeHumanized = "Renk" },
            new() { Id = 2, CategoryAttributeKey = "size", CategoryAttributeHumanized = "Beden" },
            new() { Id = 3, CategoryAttributeKey = "material", CategoryAttributeHumanized = "Malzeme" }
        };

        mockIntegrationDbContext.Setup(x => x.CategoryAttributes).ReturnsDbSet(attrs);
        var sut = CreateSut();

        // Act
        var result = await sut.SearchAttributesAsync(1, "Renk");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("Renk");
    }

    [Fact]
    public async Task SearchAttributesAsync_EmptyQuery_ReturnsAll()
    {
        // Arrange
        var attrs = new List<CategoryAttribute>
        {
            new() { Id = 1, CategoryAttributeKey = "color", CategoryAttributeHumanized = "Renk" },
            new() { Id = 2, CategoryAttributeKey = "size", CategoryAttributeHumanized = "Beden" }
        };

        mockIntegrationDbContext.Setup(x => x.CategoryAttributes).ReturnsDbSet(attrs);
        var sut = CreateSut();

        // Act
        var result = await sut.SearchAttributesAsync(1, "");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }
}
