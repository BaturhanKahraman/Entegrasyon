using Entegrasyon.Business.Concrete.Ciceksepeti;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

public class MockCiceksepetiCategoryServiceTests
{
    private readonly Mock<ILogger<MockCiceksepetiCategoryService>> _loggerMock = new();

    private MockCiceksepetiCategoryService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task GetCategoriesAsync_Should_Return_Success_With_Empty_Categories()
    {
        var sut = CreateSut();
        var result = await sut.GetCategoriesAsync();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategoryAttributesAsync_Should_Return_Success_With_Empty_Attributes()
    {
        var sut = CreateSut();
        var result = await sut.GetCategoryAttributesAsync(42);
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CategoryAttributes.Should().BeEmpty();
    }
}
