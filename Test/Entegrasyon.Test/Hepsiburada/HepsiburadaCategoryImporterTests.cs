using System.Net;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Import;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Matches;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Test.Hepsiburada;

public class HepsiburadaCategoryImporterTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IHepsiburadaApiClient> _apiClientMock = new();
    private readonly Mock<ILogger<HepsiburadaCategoryImporter>> _loggerMock = new();

    private HepsiburadaCategoryImporter CreateSut() => new(
        _apiClientMock.Object,
        mockContextFactory.Object,
        _loggerMock.Object);

    private static HttpResponseMessage CreateJsonResponse<T>(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public void Source_Should_Be_Hepsiburada()
    {
        var sut = CreateSut();
        sut.Source.Should().Be(ImportSource.Hepsiburada);
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_Should_Return_Categories_From_Api()
    {
        // Arrange
        var categories = new List<HepsiburadaCategoryDto>
        {
            new(1001, "Ayakkabı", "Spor Ayakkabı", 100,
                new[] { "Giyim", "Ayakkabı", "Spor Ayakkabı" },
                true, "ACTIVE", null, true),
            new(1002, "Tişört", "Erkek Tişört", 200,
                new[] { "Giyim", "Tişört" },
                true, "ACTIVE", null, true)
        };

        var apiResponse = new HepsiburadaApiResponse<HepsiburadaPaginatedData<HepsiburadaCategoryDto>>(
            Success: true, Code: 0, Version: 1, Message: null,
            Data: new HepsiburadaPaginatedData<HepsiburadaCategoryDto>(
                TotalElements: 2, TotalPages: 1, Number: 0,
                NumberOfElements: 2, First: true, Last: true,
                Content: categories));

        _apiClientMock
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("get-all-categories"))))
            .ReturnsAsync(CreateJsonResponse(apiResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);

        var first = result.Data!.First();
        first.ExternalId.Should().Be("1001");
        first.Name.Should().Be("Spor Ayakkabı"); // DisplayName preferred
        first.HasChildren.Should().BeFalse(); // leaf categories
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_Should_Handle_Pagination()
    {
        // Arrange — 2 pages
        var page0Categories = Enumerable.Range(1, 1000)
            .Select(i => new HepsiburadaCategoryDto(i, $"Cat{i}", $"Category {i}", null, null, true, "ACTIVE", null, true))
            .ToList();

        var page0Response = new HepsiburadaApiResponse<HepsiburadaPaginatedData<HepsiburadaCategoryDto>>(
            Success: true, Code: 0, Version: 1, Message: null,
            Data: new HepsiburadaPaginatedData<HepsiburadaCategoryDto>(
                TotalElements: 1500, TotalPages: 2, Number: 0,
                NumberOfElements: 1000, First: true, Last: false,
                Content: page0Categories));

        var page1Categories = Enumerable.Range(1001, 500)
            .Select(i => new HepsiburadaCategoryDto(i, $"Cat{i}", $"Category {i}", null, null, true, "ACTIVE", null, true))
            .ToList();

        var page1Response = new HepsiburadaApiResponse<HepsiburadaPaginatedData<HepsiburadaCategoryDto>>(
            Success: true, Code: 0, Version: 1, Message: null,
            Data: new HepsiburadaPaginatedData<HepsiburadaCategoryDto>(
                TotalElements: 1500, TotalPages: 2, Number: 1,
                NumberOfElements: 500, First: false, Last: true,
                Content: page1Categories));

        _apiClientMock
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("page=0"))))
            .ReturnsAsync(CreateJsonResponse(page0Response));

        _apiClientMock
            .Setup(x => x.GetAsync(It.Is<string>(u => u.Contains("page=1"))))
            .ReturnsAsync(CreateJsonResponse(page1Response));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1500);

        _apiClientMock.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_Should_Return_Error_On_Api_Failure()
    {
        // Arrange
        var apiResponse = new HepsiburadaApiResponse<HepsiburadaPaginatedData<HepsiburadaCategoryDto>>(
            Success: false, Code: 1001, Version: 1, Message: "Kategori bulunamadı", Data: null);

        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(apiResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Hepsiburada API hatası");
    }

    [Fact]
    public async Task GetExternalCategoriesAsync_Should_Return_Error_On_Http_Exception()
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
    public async Task GetExternalCategoriesAsync_Should_Prefer_DisplayName_Over_Name()
    {
        // Arrange
        var categories = new List<HepsiburadaCategoryDto>
        {
            new(1001, "raw_name", "Display Name", null, null, true, "ACTIVE", null, true),
            new(1002, "only_name", null, null, null, true, "ACTIVE", null, true)
        };

        var apiResponse = new HepsiburadaApiResponse<HepsiburadaPaginatedData<HepsiburadaCategoryDto>>(
            Success: true, Code: 0, Version: 1, Message: null,
            Data: new HepsiburadaPaginatedData<HepsiburadaCategoryDto>(
                TotalElements: 2, TotalPages: 1, Number: 0,
                NumberOfElements: 2, First: true, Last: true,
                Content: categories));

        _apiClientMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(CreateJsonResponse(apiResponse));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExternalCategoriesAsync();

        // Assert
        var items = result.Data!.ToList();
        items[0].Name.Should().Be("Display Name");
        items[1].Name.Should().Be("only_name");
    }
}
