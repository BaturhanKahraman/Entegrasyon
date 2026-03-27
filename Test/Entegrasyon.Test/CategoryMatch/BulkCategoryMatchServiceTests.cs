using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Business.Validation.FluentValidation;
using FluentValidation;
using FluentValidation.Results;

namespace Entegrasyon.UnitTest.CategoryMatch;

public class BulkCategoryMatchServiceTests : BaseTest
{
    private readonly CategoryMatchService _sut;

    public BulkCategoryMatchServiceTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.Validate(It.IsAny<BulkCategoryMatchDto>()))
            .ReturnsAsync(new ValidationResult());

        // Default: empty categories and marketplace links
        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(new List<Category>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new CategoryMatchService(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object);
    }

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_WithEmptyItems_ReturnsValidationError()
    {
        // Arrange
        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items = []
        };

        MockValidator
            .Setup(v => v.Validate(dto))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Items", "En az bir eşleştirme öğesi gereklidir.")
            }));

        // Act
        var result = await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("eşleştirme öğesi");
    }

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_WithInvalidMarketPlaceId_ReturnsValidationError()
    {
        // Arrange
        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 0,
            Items = [new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100 }]
        };

        MockValidator
            .Setup(v => v.Validate(dto))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("MarketPlaceId", "Geçerli bir marketplace seçilmelidir.")
            }));

        // Act
        var result = await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("marketplace");
    }

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_ExceedingMaxLimit_ReturnsValidationError()
    {
        // Arrange
        var items = Enumerable.Range(1, 501)
            .Select(i => new BulkCategoryMatchItemDto { ApplicationCategoryId = i, MarketPlaceCategoryId = i + 1000 })
            .ToList();

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items = items
        };

        MockValidator
            .Setup(v => v.Validate(dto))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Items", "En fazla 500 öğe gönderilebilir.")
            }));

        // Act
        var result = await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("500");
    }

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_WithValidItems_ReturnsSuccess()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" }
        };

        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(categories);

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items =
            [
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100, MarketPlaceCategoryName = "Electronics" },
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 2, MarketPlaceCategoryId = 200, MarketPlaceCategoryName = "Clothing" }
            ]
        };

        // Act
        var result = await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalRequested.Should().Be(2);
        result.Data.SuccessCount.Should().Be(2);
        result.Data.FailedCount.Should().Be(0);
        result.Data.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_SkipsAlreadyMappedCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" }
        };

        var existingMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true }
        };

        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(categories);
        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(existingMappings);

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items =
            [
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100, MarketPlaceCategoryName = "Electronics" },
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 2, MarketPlaceCategoryId = 200, MarketPlaceCategoryName = "Clothing" }
            ]
        };

        // Act
        var result = await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalRequested.Should().Be(2);
        result.Data.SuccessCount.Should().Be(1);
        result.Data.SkippedCount.Should().Be(1);
    }

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_InvalidCategoryId_ReportsError()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" }
        };

        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(categories);

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items =
            [
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100, MarketPlaceCategoryName = "Electronics" },
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 999, MarketPlaceCategoryId = 200, MarketPlaceCategoryName = "Unknown" }
            ]
        };

        // Act
        var result = await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.SuccessCount.Should().Be(1);
        result.Data.FailedCount.Should().Be(1);
        result.Data.Errors.Should().HaveCount(1);
        result.Data.Errors[0].ApplicationCategoryId.Should().Be(999);
    }

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_CallsSaveChangesOnce()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" },
            new() { Id = 3, Name = "Kozmetik" }
        };

        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(categories);

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items =
            [
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100 },
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 2, MarketPlaceCategoryId = 200 },
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 3, MarketPlaceCategoryId = 300 }
            ]
        };

        // Act
        await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert - single SaveChanges call for bulk operation
        mockIntegrationDbContext.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_DeletedCategory_ReportsError()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Silinmis", IsDeleted = true },
            new() { Id = 2, Name = "Aktif" }
        };

        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(categories);

        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items =
            [
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100 },
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 2, MarketPlaceCategoryId = 200 }
            ]
        };

        // Act
        var result = await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert
        result.Data.SuccessCount.Should().Be(1);
        result.Data.FailedCount.Should().Be(1);
        result.Data.Errors.Should().ContainSingle(e => e.ApplicationCategoryId == 1);
    }
}
