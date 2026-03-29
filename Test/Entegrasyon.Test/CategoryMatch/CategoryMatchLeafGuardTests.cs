using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Business.Validation.FluentValidation;
using FluentValidation.Results;

namespace Entegrasyon.UnitTest.CategoryMatch;

public class CategoryMatchLeafGuardTests : BaseTest
{
    private readonly CategoryMatchService _sut;

    // Category hierarchy for tests:
    // Parent (Id=1)  — has child → non-leaf
    //   └── Child (Id=2, SuperCategoryId=1) — no children → leaf
    // Leaf (Id=3)    — no children → leaf
    private static readonly List<Category> DefaultCategories =
    [
        new() { Id = 1, Name = "Parent", IsDeleted = false },
        new() { Id = 2, Name = "Child", SuperCategoryId = 1, IsDeleted = false },
        new() { Id = 3, Name = "Leaf", IsDeleted = false }
    ];

    public CategoryMatchLeafGuardTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.Validate(It.IsAny<BulkCategoryMatchDto>()))
            .ReturnsAsync(new ValidationResult());

        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(DefaultCategories);

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

    // ── CreateCategoryMappingAsync ────────────────────────────────

    [Fact]
    public async Task CreateCategoryMappingAsync_Should_Return_Error_When_Category_Is_Not_Leaf()
    {
        // Arrange — Parent (Id=1) has Child (Id=2) as child → non-leaf
        var dto = new CreateCategoryMarketplaceMatchDto
        {
            ApplicationCategoryId = 1,
            MarketPlaceId = 1,
            MarketPlaceCategoryId = 100,
            MarketPlaceCategoryName = "Electronics"
        };

        // Act
        var result = await _sut.CreateCategoryMappingAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("alt kategorileri");
    }

    [Fact]
    public async Task CreateCategoryMappingAsync_Should_Succeed_When_Category_Is_Leaf()
    {
        // Arrange — Leaf (Id=3) has no children → leaf
        var dto = new CreateCategoryMarketplaceMatchDto
        {
            ApplicationCategoryId = 3,
            MarketPlaceId = 1,
            MarketPlaceCategoryId = 300,
            MarketPlaceCategoryName = "Leaf Category"
        };

        // Act
        var result = await _sut.CreateCategoryMappingAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    // ── BulkCreateCategoryMappingsAsync ──────────────────────────

    [Fact]
    public async Task BulkCreateCategoryMappingsAsync_Should_Fail_NonLeaf_Categories()
    {
        // Arrange
        // Item with ApplicationCategoryId=1 → Parent (non-leaf: has child Id=2)
        // Item with ApplicationCategoryId=3 → Leaf (no children)
        var dto = new BulkCategoryMatchDto
        {
            MarketPlaceId = 1,
            Items =
            [
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 1, MarketPlaceCategoryId = 100, MarketPlaceCategoryName = "Electronics" },
                new BulkCategoryMatchItemDto { ApplicationCategoryId = 3, MarketPlaceCategoryId = 300, MarketPlaceCategoryName = "Leaf" }
            ]
        };

        // Act
        var result = await _sut.BulkCreateCategoryMappingsAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalRequested.Should().Be(2);
        result.Data.SuccessCount.Should().Be(1);
        result.Data.FailedCount.Should().Be(1);
        result.Data.Errors.Should().HaveCount(1);
        result.Data.Errors[0].ApplicationCategoryId.Should().Be(1);
        result.Data.Errors[0].ErrorMessage.Should().Contain("alt kategorileri");
    }
}
