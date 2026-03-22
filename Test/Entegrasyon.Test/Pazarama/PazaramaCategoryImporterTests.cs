using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Categories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

/// <summary>
/// PazaramaCategoryImporter için birim testleri.
/// Düz liste → ağaç dönüşümü ve API hatası senaryoları test edilir.
/// </summary>
public class PazaramaCategoryImporterTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IPazaramaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<PazaramaCategoryImporter>> _loggerMock = new();

    private PazaramaCategoryImporter CreateSut() => new(
        mockContextFactory.Object,
        _apiClientMock.Object,
        _loggerMock.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static PazaramaResponse<List<PazaramaCategoryDto>> WrapCategories(List<PazaramaCategoryDto> categories) =>
        new(Data: categories, Success: true, MessageCode: null, Message: null, UserMessage: null, FromCache: false);

    // -----------------------------------------------------------------------
    // Source property
    // -----------------------------------------------------------------------

    [Fact]
    public void Source_Should_Be_Pazarama()
    {
        var sut = CreateSut();
        sut.Source.Should().Be(ImportSource.Pazarama);
    }

    // -----------------------------------------------------------------------
    // GetExternalCategoriesAsync — flat-to-tree conversion
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetExternalCategoriesAsync_Should_Build_Tree_From_Flat_List()
    {
        // Arrange — 3 levels: root → child → leaf
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var leafId = Guid.NewGuid();

        var categories = new List<PazaramaCategoryDto>
        {
            new(rootId,  null,    null, null, "Elektronik",    null, 0, null, Leaf: false),
            new(childId, rootId,  null, null, "Telefon",       null, 0, null, Leaf: false),
            new(leafId,  childId, null, null, "Akıllı Telefon",null, 0, null, Leaf: true),
        };

        _apiClientMock
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("getCategoryTree"))))
            .ReturnsAsync(CreateJsonResponse(WrapCategories(categories)));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        var roots = result.Data!.ToList();
        roots.Should().HaveCount(1);

        var root = roots[0];
        root.ExternalId.Should().Be(rootId.ToString());
        root.Name.Should().Be("Elektronik");
        root.HasChildren.Should().BeTrue();
        root.Children.Should().HaveCount(1);

        var child = root.Children[0];
        child.ExternalId.Should().Be(childId.ToString());
        child.Name.Should().Be("Telefon");
        child.HasChildren.Should().BeTrue();
        child.Children.Should().HaveCount(1);

        var leaf = child.Children[0];
        leaf.ExternalId.Should().Be(leafId.ToString());
        leaf.Name.Should().Be("Akıllı Telefon");
        leaf.HasChildren.Should().BeFalse();
        leaf.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_Should_Return_Empty_When_Api_Returns_Empty_List()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(WrapCategories(new List<PazaramaCategoryDto>())));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_Should_Return_Error_On_Api_Failure()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_Should_Treat_Orphaned_Category_As_Root()
    {
        // Arrange — category whose parentId doesn't exist in the list
        var orphanId = Guid.NewGuid();
        var nonExistentParentId = Guid.NewGuid();

        var categories = new List<PazaramaCategoryDto>
        {
            new(orphanId, nonExistentParentId, null, null, "Yetim Kategori", null, 0, null, Leaf: true),
        };

        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(WrapCategories(categories)));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        var roots = result.Data!.ToList();
        roots.Should().HaveCount(1, "orphaned categories should be promoted to root");
        roots[0].ExternalId.Should().Be(orphanId.ToString());
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_Leaf_True_Should_Set_HasChildren_False()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var leafId = Guid.NewGuid();

        var categories = new List<PazaramaCategoryDto>
        {
            new(rootId, null,   null, null, "Kıyafet",    null, 0, null, Leaf: false),
            new(leafId, rootId, null, null, "Erkek Gömlek", null, 0, null, Leaf: true),
        };

        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(WrapCategories(categories)));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        var root = result.Data!.First();
        root.HasChildren.Should().BeTrue();  // leaf=false
        var leaf = root.Children[0];
        leaf.HasChildren.Should().BeFalse(); // leaf=true
    }
}
