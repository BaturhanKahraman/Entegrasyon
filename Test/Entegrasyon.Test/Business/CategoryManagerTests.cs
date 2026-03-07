using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using MapsterMapper;
using Moq;
using Shared.Results;
using System.Linq.Expressions;
using Xunit;
using FluentAssertions;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.EntityFrameworkCore;
using Moq.EntityFrameworkCore;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.UnitTest.Business;

public class CategoryManagerTests : BaseTest
{
    private readonly ICategoryService _categoryManager;
    private readonly Mock<ICategoryDal> _mockCategoryDal;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IFluentValidator> _mockValidator;
    private readonly Mock<IMainProductDal> _mockProductDal;
    private readonly ProductManager _productManager;

    public CategoryManagerTests()
    {
        _mockCategoryDal = new Mock<ICategoryDal>();
        _mockMapper = new Mock<IMapper>();
        _mockValidator = new Mock<IFluentValidator>();
        _mockProductDal = new Mock<IMainProductDal>();

        _productManager = new ProductManager(
            _mockProductDal.Object,
            null!, // IApplicationLogManager
            null!, // IMapper
            _mockValidator.Object,
            null!, // OfficeStockManager
            null!, // AttributeKeyValueManager
            null!  // TempBarcodeManager
        );

        _categoryManager = new CategoryManager(
            _mockCategoryDal.Object,
            mockApplicationLogger.Object,
            _mockMapper.Object,
            _mockValidator.Object,
            _productManager
        );
    }

    [Fact]
    public async Task AddCategory_Should_Add_Category_And_Return_Success()
    {
        // Arrange
        var dto = new AddCategoryDto("Test Category", new List<AddCategoryAttributeDto>(), null, false);
        var category = new Category { Id = 1, Name = "Test Category" };
        var detailDto = new CategoryDetailDto(1, 0, "Test Category", 0, false, 0, "");

        _mockMapper.Setup(m => m.Map<Category>(dto)).Returns(category);
        _mockCategoryDal.Setup(d => d.AddAsync(category)).Returns(Task.CompletedTask);
        _mockCategoryDal.Setup(d => d.ConvertToCategoryDetail(category)).ReturnsAsync(detailDto);

        // Act
        var result = await _categoryManager.AddCategory(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Name.Should().Be("Test Category");
        _mockCategoryDal.Verify(d => d.AddAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task SoftDelete_Should_Return_Error_If_Products_Exist()
    {
        // Arrange
        int categoryId = 1;
        var category = new Category { Id = categoryId, Name = "Test" };

        _mockCategoryDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Category, bool>>>(), false)).ReturnsAsync(category);

        var products = new List<Product> { new Product { Id = Guid.NewGuid(), CategoryId = categoryId } };
        _mockProductDal.Setup(d => d.Table).ReturnsDbSet(products);

        // Act
        var result = await _categoryManager.SoftDelete(categoryId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be(Messages.CategoryHasProducts);
        _mockCategoryDal.Verify(d => d.SoftDeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task SoftDelete_Should_Succeed_If_No_Products_Exist()
    {
        // Arrange
        int categoryId = 1;
        var category = new Category { Id = categoryId, Name = "Test" };

        _mockCategoryDal.Setup(d => d.GetAsync(It.IsAny<Expression<Func<Category, bool>>>(), false)).ReturnsAsync(category);

        var products = new List<Product>();
        _mockProductDal.Setup(d => d.Table).ReturnsDbSet(products);
        _mockCategoryDal.Setup(d => d.SoftDeleteAsync(category)).Returns(Task.CompletedTask);

        // Act
        var result = await _categoryManager.SoftDelete(categoryId);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be(Messages.CategoryDeleted);
        _mockCategoryDal.Verify(d => d.SoftDeleteAsync(category), Times.Once);
    }
}
