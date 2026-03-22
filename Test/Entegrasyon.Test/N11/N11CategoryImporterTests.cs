using System.Xml.Linq;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.Entity.Categories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

/// <summary>
/// N11CategoryImporter için birim testleri.
/// </summary>
public class N11CategoryImporterTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IN11SoapClient> _soapClientMock = new();
    private readonly Mock<ILogger<N11CategoryImporter>> _loggerMock = new();

    private const string N11Namespace = "http://www.n11.com/ws/schemas";

    private N11CategoryImporter CreateSut() => new(
        mockContextFactory.Object,
        _soapClientMock.Object,
        _loggerMock.Object);

    // -----------------------------------------------------------------------
    // GetExternalCategoriesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetExternalCategoriesAsync_ShouldReturnTopLevelCategories()
    {
        // Arrange
        var responseXml = XElement.Parse("""
            <GetTopLevelCategoriesResponse xmlns="http://www.n11.com/ws/schemas">
              <result><status>success</status></result>
              <categories>
                <category>
                  <id>1001</id>
                  <name>Elektronik</name>
                </category>
                <category>
                  <id>1002</id>
                  <name>Giyim</name>
                </category>
              </categories>
            </GetTopLevelCategoriesResponse>
            """);

        _soapClientMock
            .Setup(s => s.SendAsync("CategoryService", "", It.IsAny<XElement>()))
            .ReturnsAsync(responseXml);

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var categories = result.Data!.ToList();
        categories.Should().HaveCount(2);
        categories[0].ExternalId.Should().Be("1001");
        categories[0].Name.Should().Be("Elektronik");
        categories[0].HasChildren.Should().BeTrue();
        categories[1].ExternalId.Should().Be("1002");
        categories[1].Name.Should().Be("Giyim");
        categories[1].HasChildren.Should().BeTrue();
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_WhenSoapFails_ShouldReturnError()
    {
        // Arrange
        _soapClientMock
            .Setup(s => s.SendAsync("CategoryService", "", It.IsAny<XElement>()))
            .ThrowsAsync(new HttpRequestException("SOAP bağlantı hatası"));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrEmpty();
    }

    // -----------------------------------------------------------------------
    // GetSubCategoriesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetSubCategoriesAsync_ShouldReturnChildCategories()
    {
        // Arrange
        var responseXml = XElement.Parse("""
            <GetSubCategoriesResponse xmlns="http://www.n11.com/ws/schemas">
              <result><status>success</status></result>
              <category>
                <id>1001</id>
                <name>Elektronik</name>
                <subCategoryList>
                  <subCategory>
                    <id>2001</id>
                    <name>Telefon</name>
                  </subCategory>
                  <subCategory>
                    <id>2002</id>
                    <name>Bilgisayar</name>
                  </subCategory>
                </subCategoryList>
              </category>
            </GetSubCategoriesResponse>
            """);

        _soapClientMock
            .Setup(s => s.SendAsync("CategoryService", "", It.IsAny<XElement>()))
            .ReturnsAsync(responseXml);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSubCategoriesAsync(1001);

        // Assert
        result.Should().HaveCount(2);
        result[0].ExternalId.Should().Be("2001");
        result[0].Name.Should().Be("Telefon");
        result[0].HasChildren.Should().BeTrue();
        result[1].ExternalId.Should().Be("2002");
        result[1].Name.Should().Be("Bilgisayar");
        result[1].HasChildren.Should().BeTrue();
    }

    [Fact]
    public async Task GetSubCategoriesAsync_WhenLeafNode_ShouldReturnEmpty()
    {
        // Arrange — SOAP yanıtında subCategoryList yoktur (yaprak kategori)
        var responseXml = XElement.Parse("""
            <GetSubCategoriesResponse xmlns="http://www.n11.com/ws/schemas">
              <result><status>success</status></result>
              <category>
                <id>9999</id>
                <name>Yaprak Kategori</name>
              </category>
            </GetSubCategoriesResponse>
            """);

        _soapClientMock
            .Setup(s => s.SendAsync("CategoryService", "", It.IsAny<XElement>()))
            .ReturnsAsync(responseXml);

        var sut = CreateSut();

        // Act
        var result = await sut.GetSubCategoriesAsync(9999);

        // Assert
        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Yapısal / Tip kontrolü
    // -----------------------------------------------------------------------

    [Fact]
    public void N11CategoryImporter_ShouldExtend_BaseCategoryImporterService()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.Should().BeAssignableTo<BaseCategoryImporterService>();
        sut.Should().BeAssignableTo<ICategoryImporterService>();
    }

    [Fact]
    public void N11CategoryImporter_Source_ShouldBe_N11()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.Source.Should().Be(ImportSource.N11);
    }
}
