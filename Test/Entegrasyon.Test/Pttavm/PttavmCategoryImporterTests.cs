using System.Net;
using System.Text;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pttavm;

public class PttavmCategoryImporterTests
{
    private readonly Mock<IPttavmCatalogApiClient> _apiClientMock;
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactoryMock;
    private readonly Mock<ILogger<PttavmCategoryImporter>> _loggerMock;
    private readonly PttavmCategoryImporter _sut;

    public PttavmCategoryImporterTests()
    {
        _apiClientMock = new Mock<IPttavmCatalogApiClient>();
        _contextFactoryMock = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _loggerMock = new Mock<ILogger<PttavmCategoryImporter>>();

        _sut = new PttavmCategoryImporter(
            _contextFactoryMock.Object,
            _apiClientMock.Object,
            _loggerMock.Object);
    }

    private void SetupApiClientMainCategories(string json)
    {
        _apiClientMock
            .Setup(c => c.GetAsync(It.Is<string>(url => url.Contains("categories/main"))))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }

    [Fact]
    public void Source_ShouldBePttavm()
    {
        _sut.Source.Should().Be(ImportSource.Pttavm);
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldReturnMainCategories()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "main_category": [
                { "id": "100", "name": "Elektronik" },
                { "id": "200", "name": "Giyim" }
            ]
        }
        """;
        SetupApiClientMainCategories(json);

        // Act
        var result = await _sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        var categories = result.Data!.ToList();
        categories.Should().HaveCount(2);
        categories[0].ExternalId.Should().Be("100");
        categories[0].Name.Should().Be("Elektronik");
        categories[0].HasChildren.Should().BeTrue();
        categories[1].ExternalId.Should().Be("200");
        categories[1].Name.Should().Be("Giyim");
        categories[1].HasChildren.Should().BeTrue();
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldReturnEmptyList_WhenApiReturnsNoCategories()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "main_category": []
        }
        """;
        SetupApiClientMainCategories(json);

        // Act
        var result = await _sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldReturnError_WhenApiReturnsFailure()
    {
        // Arrange
        var json = """
        {
            "success": false,
            "error": {
                "error_code": "ERR001",
                "error_message": "Yetkisiz erisim"
            }
        }
        """;
        SetupApiClientMainCategories(json);

        // Act
        var result = await _sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task LoadChildrenAsync_ShouldReturnChildren()
    {
        // Arrange
        var json = """
        {
            "success": true,
            "category": {
                "id": "1",
                "name": "Elektronik",
                "children": [
                    { "id": "10", "name": "Telefon", "parent_id": "1", "children": [] },
                    { "id": "20", "name": "Bilgisayar", "parent_id": "1", "children": [{ "id": "21", "name": "Laptop", "parent_id": "20" }] }
                ]
            }
        }
        """;
        _apiClientMock
            .Setup(c => c.GetAsync(It.Is<string>(url => url.Contains("categories/1"))))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _sut.LoadChildrenAsync("1");

        // Assert
        result.Should().HaveCount(2);
        result[0].ExternalId.Should().Be("10");
        result[0].Name.Should().Be("Telefon");
        result[0].ParentExternalId.Should().Be("1");
        result[0].HasChildren.Should().BeFalse();
        result[1].ExternalId.Should().Be("20");
        result[1].Name.Should().Be("Bilgisayar");
        result[1].ParentExternalId.Should().Be("1");
        result[1].HasChildren.Should().BeTrue();
    }
}
