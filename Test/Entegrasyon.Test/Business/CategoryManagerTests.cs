using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

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
        var notificationFlags = Options.Create(new NotificationFeatureFlags { PublishEnabled = false });
        var currentUser = new Mock<ICurrentUserContext>();
        _categoryManager = new CategoryManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            _categoryMapper,
            MockValidator.Object,
            _mockProductService.Object,
            tenantCache,
            mockHybridCache.Object,
            mockTenantContext.Object,
            notificationFlags,
            currentUser.Object
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
   
    // AddCategory parent guard tests — üst kategorinin pazaryeri EŞLEŞMESİ alt kategori
    // eklemeyi ENGELLEMEZ (kullanıcı kararı: parent'ta eşleşmeye bakılmaz). Özellik kuralı korunur.
    [Fact]
    public async Task AddCategory_Should_Succeed_When_Parent_Has_Active_MarketplaceSync()
    {
        // Arrange — üst kategorinin aktif pazaryeri eşleşmesi var ama özelliği yok
        var dto = new AddCategoryDto("Child", [], 10, false);

        IList<CategoryAttributeCategory> attrCategories = [];
        mockIntegrationDbContext.Setup(x => x.CategoryAttributeCategories).ReturnsDbSet(attrCategories);

        IList<CategoryMarketplace> marketplaceLinks =
        [
            new CategoryMarketplace { CategoryId = 10, MarketPlaceId = 1, IsActive = true }
        ];
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(marketplaceLinks);

        IList<Category> categories = [new Category { Id = 10, Name = "Parent" }];
        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _categoryManager.AddCategory(dto);

        // Assert — eşleşme artık engellemiyor
        result.Success.Should().BeTrue();
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

    // Not: UpdateCategory'de de üst kategori pazaryeri eşleşmesi engeli kaldırıldı (kullanıcı kararı).
    // Eski "parent active sync → hata" testi silindi; success path xmin Entry() nedeniyle unit'te
    // mock'lanamadığından integration kapsamında doğrulanır.

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
