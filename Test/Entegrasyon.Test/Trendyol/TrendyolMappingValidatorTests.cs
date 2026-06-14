using Entegrasyon.Business.Concrete.Trendyol;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolMappingValidator unit tests — verifies product mapping validation
/// for category, brand, and required attribute mappings.
/// </summary>
public class TrendyolMappingValidatorTests : Entegrasyon.UnitTest.BaseTest
{
    private static readonly Guid TestProductId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private TrendyolMappingValidator CreateSut() => new(mockContextFactory.Object);

    private void SetupProduct(int categoryId = 1, int? brandId = 10, string title = "Test Urun")
    {
        var product = new Product
        {
            Id = TestProductId,
            Title = title,
            CategoryId = categoryId,
            BrandId = brandId
        };
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product> { product });
    }

    private void SetupCategoryMatch(int categoryId, bool exists)
    {
        var matches = exists
            ? new List<CategoryMarketplace>
            {
                new() { CategoryId = categoryId, MarketPlaceId = TrendyolMarketPlaceId, MarketPlaceCategoryId = 100, IsActive = true }
            }
            : new List<CategoryMarketplace>();

        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(matches);
    }

    private void SetupBrandMatch(int brandId, bool exists)
    {
        var matches = exists
            ? new List<BrandMarketPlaceMatch>
            {
                new() { ApplicationBrandId = brandId, MarketPlaceId = TrendyolMarketPlaceId, MarketPlaceBrandId = 200 }
            }
            : new List<BrandMarketPlaceMatch>();

        mockIntegrationDbContext.Setup(x => x.BrandMarketPlaceMatches).ReturnsDbSet(matches);
    }

    private void SetupRequiredAttributes(int categoryId, List<int>? requiredAttrIds = null, List<int>? mappedAttrIds = null)
    {
        var cacs = (requiredAttrIds ?? new List<int>())
            .Select(id => new CategoryAttributeCategory { CategoryId = categoryId, CategoryAttributeId = id, IsRequired = true })
            .ToList();

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(cacs);

        var attrMatches = (mappedAttrIds ?? new List<int>())
            .Select(id => new CategoryAttributeMarketPlaceMatch
            {
                ApplicationCategoryAttributeId = id,
                MarketPlaceId = TrendyolMarketPlaceId,
                MarketPlaceCategoryAttributeId = id * 10
            })
            .ToList();

        mockIntegrationDbContext.Setup(x => x.CategoryAttributeMarketPlaceMatches).ReturnsDbSet(attrMatches);
    }

    // ── Test 1: All mappings exist — success ──

    [Fact]
    public async Task ValidateProductMappingsAsync_AllMappingsExist_ReturnsSuccess()
    {
        // Arrange
        SetupProduct(categoryId: 1, brandId: 10);
        SetupCategoryMatch(1, exists: true);
        SetupBrandMatch(10, exists: true);
        SetupRequiredAttributes(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
    }

    // ── Test 2: Product not found ──

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenProductNotFound_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.MainProducts).ReturnsDbSet(new List<Product>());
        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    // ── Test 3: Category match missing ──

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenCategoryNotMapped_ReturnsError()
    {
        // Arrange
        SetupProduct(categoryId: 1, brandId: 10);
        SetupCategoryMatch(1, exists: false);
        SetupBrandMatch(10, exists: true);
        SetupRequiredAttributes(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("kategorisi");
    }

    // ── Test 4: Brand ID is null ──

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenBrandIdNull_ReturnsError()
    {
        // Arrange
        SetupProduct(categoryId: 1, brandId: null);
        SetupCategoryMatch(1, exists: true);
        SetupBrandMatch(0, exists: false);
        SetupRequiredAttributes(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("markasi belirlenmemis");
    }

    // ── Test 5: Brand match missing ──

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenBrandNotMapped_ReturnsError()
    {
        // Arrange
        SetupProduct(categoryId: 1, brandId: 10);
        SetupCategoryMatch(1, exists: true);
        SetupBrandMatch(10, exists: false);
        SetupRequiredAttributes(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("markasi Trendyol");
    }

    // ── Test 6: Required attributes not mapped ──

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenRequiredAttrsNotMapped_ReturnsError()
    {
        // Arrange
        SetupProduct(categoryId: 1, brandId: 10);
        SetupCategoryMatch(1, exists: true);
        SetupBrandMatch(10, exists: true);
        SetupRequiredAttributes(1, requiredAttrIds: [100, 101, 102], mappedAttrIds: [100]);

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("2 zorunlu ozellik");
    }

    // ── Test 7: All required attributes mapped — success ──

    [Fact]
    public async Task ValidateProductMappingsAsync_WhenAllRequiredAttrsMapped_ReturnsSuccess()
    {
        // Arrange
        SetupProduct(categoryId: 1, brandId: 10);
        SetupCategoryMatch(1, exists: true);
        SetupBrandMatch(10, exists: true);
        SetupRequiredAttributes(1, requiredAttrIds: [100, 101], mappedAttrIds: [100, 101]);

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(TestProductId);

        // Assert
        result.Success.Should().BeTrue();
    }

    // ── Test 8: Multiple errors combined ──

    [Fact]
    public async Task ValidateProductMappingsAsync_MultipleErrors_CombinedInMessage()
    {
        // Arrange
        SetupProduct(categoryId: 1, brandId: null);
        SetupCategoryMatch(1, exists: false);
        SetupBrandMatch(0, exists: false);
        SetupRequiredAttributes(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ValidateProductMappingsAsync(TestProductId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("kategorisi");
        result.Message.Should().Contain("markasi");
    }
}
