using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Moq;
using Xunit;
using FluentAssertions;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.Extensions.Caching.Memory;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class CategoryManagerTests : BaseTest
{
    private readonly ICategoryService _categoryManager;
    private readonly CategoryMapper _categoryMapper = new();
    private readonly Mock<IProductService> _mockProductService = new();

    public CategoryManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        var tenantCache = new TenantMemoryCache(new MemoryCache(new MemoryCacheOptions()), mockTenantContext.Object);
        _categoryManager = new CategoryManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            _categoryMapper,
            MockValidator.Object,
            _mockProductService.Object,
            tenantCache
        );
    }

    // IsLeafCategoryAsync tests
    [Fact]
    public async Task IsLeafCategoryAsync_Should_Return_True_When_Category_Has_No_Children()
    {
        // Arrange
        int categoryId = 10;
        IList<Category> categories =
        [
            new Category { Id = categoryId, Name = "Leaf" }
            // No child with SuperCategoryId == categoryId
        ];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        // Act
        var result = await _categoryManager.IsLeafCategoryAsync(categoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsLeafCategoryAsync_Should_Return_False_When_Category_Has_Children()
    {
        // Arrange
        int categoryId = 20;
        IList<Category> categories =
        [
            new Category { Id = categoryId, Name = "Parent" },
            new Category { Id = 21, Name = "Child", SuperCategoryId = categoryId }
        ];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        // Act
        var result = await _categoryManager.IsLeafCategoryAsync(categoryId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLeafCategoryAsync_Should_Return_True_When_Only_Deleted_Children_Exist()
    {
        // Arrange
        int categoryId = 30;
        IList<Category> categories =
        [
            new Category { Id = categoryId, Name = "Parent" },
            new Category { Id = 31, Name = "DeletedChild", SuperCategoryId = categoryId, IsDeleted = true }
        ];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        // Act
        var result = await _categoryManager.IsLeafCategoryAsync(categoryId);

        // Assert
        result.Should().BeTrue();
    }

    // GetValidParentCandidatesAsync sync filter tests
    [Fact]
    public async Task GetValidParentCandidatesAsync_Should_Exclude_Categories_With_Active_MarketplaceSync()
    {
        // Arrange
        IList<Category> categories =
        [
            new Category { Id = 1, Name = "NoSync" },
            new Category
            {
                Id = 2,
                Name = "HasActiveSync",
                MarketplaceLinks = new List<CategoryMarketplace>
                {
                    new CategoryMarketplace { CategoryId = 2, MarketPlaceId = 1, IsActive = true }
                }
            }
        ];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        // Act
        var result = await _categoryManager.GetValidParentCandidatesAsync();

        // Assert
        result.Should().NotContain(c => c.Id == 2);
        result.Should().Contain(c => c.Id == 1);
    }

    [Fact]
    public async Task GetValidParentCandidatesAsync_Should_Include_Categories_With_Only_Inactive_MarketplaceSync()
    {
        // Arrange
        IList<Category> categories =
        [
            new Category
            {
                Id = 3,
                Name = "InactiveSync",
                MarketplaceLinks = new List<CategoryMarketplace>
                {
                    new CategoryMarketplace { CategoryId = 3, MarketPlaceId = 1, IsActive = false }
                }
            }
        ];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        // Act
        var result = await _categoryManager.GetValidParentCandidatesAsync();

        // Assert
        result.Should().Contain(c => c.Id == 3);
    }

    [Fact]
    public async Task GetValidParentCandidatesAsync_WithExclude_Should_Exclude_Categories_With_Active_MarketplaceSync()
    {
        // Arrange
        IList<Category> categories =
        [
            new Category { Id = 1, Name = "NoSync" },
            new Category
            {
                Id = 2,
                Name = "HasActiveSync",
                MarketplaceLinks = new List<CategoryMarketplace>
                {
                    new CategoryMarketplace { CategoryId = 2, MarketPlaceId = 1, IsActive = true }
                }
            },
            new Category { Id = 5, Name = "Excluded" }
        ];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        // Act
        var result = await _categoryManager.GetValidParentCandidatesAsync(excludeCategoryId: 5);

        // Assert
        result.Should().NotContain(c => c.Id == 2);
        result.Should().NotContain(c => c.Id == 5);
        result.Should().Contain(c => c.Id == 1);
    }

    // AddCategory sync guard tests
    [Fact]
    public async Task AddCategory_Should_Return_Error_When_Parent_Has_Active_MarketplaceSync()
    {
        // Arrange
        var dto = new AddCategoryDto("Child", [], 10, false);

        IList<CategoryAttributeCategory> attrCategories = [];
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);

        IList<CategoryMarketplace> marketplaceLinks =
        [
            new CategoryMarketplace { CategoryId = 10, MarketPlaceId = 1, IsActive = true }
        ];
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceLinks);

        // Act
        var result = await _categoryManager.AddCategory(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("pazar yeri eşleştirmesi");
    }

    [Fact]
    public async Task AddCategory_Should_Succeed_When_Parent_Has_No_Active_MarketplaceSync()
    {
        // Arrange
        var dto = new AddCategoryDto("Child", [], 10, false);

        IList<CategoryAttributeCategory> attrCategories = [];
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);

        IList<CategoryMarketplace> marketplaceLinks =
        [
            new CategoryMarketplace { CategoryId = 10, MarketPlaceId = 1, IsActive = false }
        ];
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceLinks);

        IList<Category> categories = [new Category { Id = 10, Name = "Parent" }];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _categoryManager.AddCategory(dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    // UpdateCategory sync guard tests
    [Fact]
    public async Task UpdateCategory_Should_Return_Error_When_Parent_Has_Active_MarketplaceSync()
    {
        // Arrange
        var dto = new EditCategoryDto(5, "Child", 10, false, false);

        IList<Category> categories = [new Category { Id = 5, Name = "Child" }];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);

        IList<CategoryAttributeCategory> attrCategories = [];
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);

        IList<CategoryMarketplace> marketplaceLinks =
        [
            new CategoryMarketplace { CategoryId = 10, MarketPlaceId = 1, IsActive = true }
        ];
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceLinks);

        // Act
        var result = await _categoryManager.UpdateCategory(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("pazar yeri eşleştirmesi");
    }

    [Fact]
    public async Task SoftDelete_Should_Return_Error_If_Products_Exist()
    {
        // Arrange
        int categoryId = 1;
        IList<Category> categories = [new Category { Id = categoryId, Name = "Test" }];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        _mockProductService.Setup(p => p.GetProductCountByCategoryId(categoryId)).ReturnsAsync(1);

        // Act
        var result = await _categoryManager.SoftDelete(categoryId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(Messages.CategoryHasProducts);
    }

    [Fact]
    public async Task SoftDelete_Should_Succeed_If_No_Products_Exist()
    {
        // Arrange
        int categoryId = 2;
        IList<Category> categories = [new Category { Id = categoryId, Name = "Test2" }];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockProductService.Setup(p => p.GetProductCountByCategoryId(categoryId)).ReturnsAsync(0);

        // Act
        var result = await _categoryManager.SoftDelete(categoryId);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.CategoryDeleted);
    }
}
