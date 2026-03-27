using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.UnitTest.CategoryMatch;

public class BulkCategoryMatchDtoValidatorTests
{
    private readonly BulkCategoryMatchDtoValidator _validator = new();

    [Fact]
    public async Task Validate_ValidDto_Passes()
    {
        // Arrange
        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items =
            [
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100 }
            ]
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ZeroMarketPlaceId_Fails()
    {
        // Arrange
        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 0,
            Items = [new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100 }]
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MarketPlaceId");
    }

    [Fact]
    public async Task Validate_EmptyItems_Fails()
    {
        // Arrange
        var dto = new BulkCategoryMatchDto { MarketPlaceId = 1, Items = [] };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public async Task Validate_TooManyItems_Fails()
    {
        // Arrange
        var items = Enumerable.Range(1, 501)
            .Select(i => new BulkCategoryMatchItemDto { ApplicationCategoryId = i, MarketPlaceCategoryId = i })
            .ToList();
        var dto = new BulkCategoryMatchDto { MarketPlaceId = 1, Items = items };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public async Task Validate_ItemWithZeroApplicationCategoryId_Fails()
    {
        // Arrange
        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items = [new BulkCategoryMatchItemDto { ApplicationCategoryId = 0, MarketPlaceCategoryId = 100 }]
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicationCategoryId"));
    }

    [Fact]
    public async Task Validate_ItemWithZeroMarketPlaceCategoryId_Fails()
    {
        // Arrange
        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items = [new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 0 }]
        };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("MarketPlaceCategoryId"));
    }

    [Fact]
    public async Task Validate_500Items_Passes()
    {
        // Arrange
        var items = Enumerable.Range(1, 500)
            .Select(i => new BulkCategoryMatchItemDto { ApplicationCategoryId = i, MarketPlaceCategoryId = i })
            .ToList();
        var dto = new BulkCategoryMatchDto { MarketPlaceId = 1, Items = items };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
