using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Concrete.Temu;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Temu;

/// <summary>
/// TemuCategoryImporter unit tests — verifies category tree fetching and ImportSource.
/// </summary>
public class TemuCategoryImporterTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<ITemuApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<TemuCategoryImporter>> _loggerMock = new();

    private TemuCategoryImporter CreateSut() => new(
        mockContextFactory.Object,
        _apiClientMock.Object,
        _loggerMock.Object);

    [Fact]
    public void Source_ShouldReturnTemuImportSource()
    {
        var sut = CreateSut();
        sut.Source.Should().Be(ImportSource.Temu);
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ReturnsCategories_WhenApiSucceeds()
    {
        // Arrange
        var mockCategories = new TemuCategoryListResponse
        {
            CatList = new List<TemuCategoryDto>
            {
                new()
                {
                    CatId = 100,
                    CatName = "Elektronik",
                    ParentCatId = 0,
                    Leaf = false,
                    Children = new List<TemuCategoryDto>
                    {
                        new()
                        {
                            CatId = 101,
                            CatName = "Telefon",
                            ParentCatId = 100,
                            Leaf = true,
                            Children = new List<TemuCategoryDto>()
                        }
                    }
                },
                new()
                {
                    CatId = 200,
                    CatName = "Giyim",
                    ParentCatId = 0,
                    Leaf = false,
                    Children = new List<TemuCategoryDto>()
                }
            }
        };

        _apiClientMock
            .Setup(c => c.CallAsync<TemuCategoryListResponse>(
                "bg.goods.cats.get",
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCategories);

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var categories = result.Data!.ToList();
        categories.Should().HaveCount(2);
        categories[0].Name.Should().Be("Elektronik");
        categories[0].Children.Should().HaveCount(1);
        categories[0].Children[0].Name.Should().Be("Telefon");
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ReturnsError_WhenApiThrows()
    {
        // Arrange
        _apiClientMock
            .Setup(c => c.CallAsync<TemuCategoryListResponse>(
                "bg.goods.cats.get",
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TemuApiException(1001, "Test error"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Test error");
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        var emptyResponse = new TemuCategoryListResponse
        {
            CatList = new List<TemuCategoryDto>()
        };

        _apiClientMock
            .Setup(c => c.CallAsync<TemuCategoryListResponse>(
                "bg.goods.cats.get",
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResponse);

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_MapsLeafNodeCorrectly()
    {
        // Arrange
        var response = new TemuCategoryListResponse
        {
            CatList = new List<TemuCategoryDto>
            {
                new()
                {
                    CatId = 999,
                    CatName = "Leaf Kategori",
                    ParentCatId = 0,
                    Leaf = true,
                    Children = new List<TemuCategoryDto>()
                }
            }
        };

        _apiClientMock
            .Setup(c => c.CallAsync<TemuCategoryListResponse>(
                "bg.goods.cats.get",
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        var cats = result.Data!.ToList();
        cats.Should().HaveCount(1);
        cats[0].ExternalId.Should().Be("999");
        cats[0].HasChildren.Should().BeFalse();
        cats[0].Children.Should().BeEmpty();
    }
}
