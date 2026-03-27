using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Business.Validation.FluentValidation;
using FluentValidation.Results;

namespace Entegrasyon.UnitTest.CategoryMatch;

public class CategoryMatchTemplateTests : BaseTest
{
    private readonly CategoryMatchService _sut;

    public CategoryMatchTemplateTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.Validate(It.IsAny<BulkCategoryMatchDto>()))
            .ReturnsAsync(new ValidationResult());

        mockIntegrationDbContext
            .Setup(x => x.Categories)
            .ReturnsDbSet(new List<Category>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryMarketplaces)
            .ReturnsDbSet(new List<CategoryMarketplace>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryMatchTemplates)
            .ReturnsDbSet(new List<CategoryMatchTemplate>());
        mockIntegrationDbContext
            .Setup(x => x.CategoryMatchTemplateItems)
            .ReturnsDbSet(new List<CategoryMatchTemplateItem>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new CategoryMatchService(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object);
    }

    [Fact]
    public async Task SaveTemplateAsync_WithValidData_CreatesTemplate()
    {
        // Arrange
        var existingMappings = new List<CategoryMarketplace>
        {
            new() { CategoryId = 1, MarketPlaceId = 1, IsActive = true, MarketPlaceCategoryId = 100, MarketPlaceCategoryName = "Elektronik", Category = new Category { Id = 1, Name = "Elektronik" } },
            new() { CategoryId = 2, MarketPlaceId = 1, IsActive = true, MarketPlaceCategoryId = 200, MarketPlaceCategoryName = "Giyim", Category = new Category { Id = 2, Name = "Giyim" } }
        };
        mockIntegrationDbContext.Setup(x => x.CategoryMarketplaces).ReturnsDbSet(existingMappings);

        var dto = new SaveCategoryMatchTemplateDto
        {
            Name = "Test Template",
            Description = "Test aciklama",
            MarketPlaceId = 1
        };

        // Act
        var result = await _sut.SaveTemplateAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SaveTemplateAsync_WithEmptyName_ReturnsError()
    {
        // Arrange
        var dto = new SaveCategoryMatchTemplateDto
        {
            Name = "",
            MarketPlaceId = 1
        };

        // Act
        var result = await _sut.SaveTemplateAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ad");
    }

    [Fact]
    public async Task SaveTemplateAsync_NoMappingsExist_ReturnsError()
    {
        // Arrange - no mappings in DB
        var dto = new SaveCategoryMatchTemplateDto
        {
            Name = "Empty Template",
            MarketPlaceId = 1
        };

        // Act
        var result = await _sut.SaveTemplateAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("eslestirme");
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsTemplatesForMarketplace()
    {
        // Arrange
        var templates = new List<CategoryMatchTemplate>
        {
            new() { Id = 1, Name = "Template 1", MarketPlaceId = 1, Items = new List<CategoryMatchTemplateItem>
            {
                new() { Id = 1, TemplateId = 1, ApplicationCategoryId = 1, MarketPlaceCategoryId = 100 }
            }},
            new() { Id = 2, Name = "Template 2", MarketPlaceId = 2, Items = [] }
        };
        mockIntegrationDbContext.Setup(x => x.CategoryMatchTemplates).ReturnsDbSet(templates);

        // Act
        var result = await _sut.GetTemplatesAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Name.Should().Be("Template 1");
        result.Data[0].MappingCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTemplateDetailAsync_ReturnsDetailWithMappings()
    {
        // Arrange
        var templates = new List<CategoryMatchTemplate>
        {
            new()
            {
                Id = 1, Name = "Detail Template", MarketPlaceId = 1, Description = "Detayli",
                Items = new List<CategoryMatchTemplateItem>
                {
                    new() { Id = 1, TemplateId = 1, ApplicationCategoryId = 1, ApplicationCategoryName = "Elektronik", MarketPlaceCategoryId = 100, MarketPlaceCategoryName = "Electronics" }
                }
            }
        };
        mockIntegrationDbContext.Setup(x => x.CategoryMatchTemplates).ReturnsDbSet(templates);

        // Act
        var result = await _sut.GetTemplateDetailAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Name.Should().Be("Detail Template");
        result.Data.Mappings.Should().HaveCount(1);
        result.Data.Mappings[0].ApplicationCategoryName.Should().Be("Elektronik");
    }

    [Fact]
    public async Task GetTemplateDetailAsync_NotFound_ReturnsError()
    {
        // Arrange - no templates
        // Act
        var result = await _sut.GetTemplateDetailAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task DeleteTemplateAsync_ExistingTemplate_Succeeds()
    {
        // Arrange
        var templates = new List<CategoryMatchTemplate>
        {
            new() { Id = 1, Name = "To Delete", MarketPlaceId = 1, Items = [] }
        };
        mockIntegrationDbContext.Setup(x => x.CategoryMatchTemplates).ReturnsDbSet(templates);

        // Act
        var result = await _sut.DeleteTemplateAsync(1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTemplateAsync_NotFound_ReturnsError()
    {
        // Act
        var result = await _sut.DeleteTemplateAsync(999);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyTemplateAsync_AppliesTemplateMappings()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Elektronik" },
            new() { Id = 2, Name = "Giyim" }
        };
        var templates = new List<CategoryMatchTemplate>
        {
            new()
            {
                Id = 1, Name = "Apply Template", MarketPlaceId = 1,
                Items = new List<CategoryMatchTemplateItem>
                {
                    new() { TemplateId = 1, ApplicationCategoryId = 1, MarketPlaceCategoryId = 100, MarketPlaceCategoryName = "Electronics" },
                    new() { TemplateId = 1, ApplicationCategoryId = 2, MarketPlaceCategoryId = 200, MarketPlaceCategoryName = "Clothing" }
                }
            }
        };

        mockIntegrationDbContext.Setup(x => x.Categories).ReturnsDbSet(categories);
        mockIntegrationDbContext.Setup(x => x.CategoryMatchTemplates).ReturnsDbSet(templates);

        // Act
        var result = await _sut.ApplyTemplateAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalRequested.Should().Be(2);
        result.Data.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task ApplyTemplateAsync_NotFound_ReturnsError()
    {
        // Act
        var result = await _sut.ApplyTemplateAsync(999);

        // Assert
        result.Success.Should().BeFalse();
    }
}
