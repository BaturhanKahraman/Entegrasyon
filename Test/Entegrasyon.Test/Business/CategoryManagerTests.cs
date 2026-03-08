using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using MapsterMapper;
using Moq;
using Shared.Results;
using Xunit;
using FluentAssertions;
using Entegrasyon.Business.Utility.Constants;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class CategoryManagerTests : BaseTest
{
    private readonly ICategoryService _categoryManager;
    private readonly Mock<IMapper> _mockMapper = new();
    private readonly Mock<IProductService> _mockProductService = new();

    public CategoryManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        _categoryManager = new CategoryManager(
            mockIntegrationDbContext.Object,
            mockApplicationLogger.Object,
            _mockMapper.Object,
            MockValidator.Object,
            _mockProductService.Object,
            mockMemoryCache.Object
        );
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
