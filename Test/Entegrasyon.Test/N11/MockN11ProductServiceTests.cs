using Entegrasyon.Business.Concrete.N11;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

public class MockN11ProductServiceTests
{
    private readonly Mock<ILogger<MockN11ProductService>> _loggerMock = new();

    [Fact]
    public async Task SaveProductAsync_Should_Return_Success_With_MockId()
    {
        var sut = new MockN11ProductService(_loggerMock.Object);
        var result = await sut.SaveProductAsync(Guid.NewGuid());
        result.Success.Should().BeTrue();
        result.Data.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteProductAsync_Should_Return_Success()
    {
        var sut = new MockN11ProductService(_loggerMock.Object);
        var result = await sut.DeleteProductAsync(Guid.NewGuid());
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SaveProductAsync_Should_Return_Different_Ids_For_Different_Products()
    {
        var sut = new MockN11ProductService(_loggerMock.Object);
        var result1 = await sut.SaveProductAsync(Guid.NewGuid());
        var result2 = await sut.SaveProductAsync(Guid.NewGuid());
        result1.Data.Should().NotBe(result2.Data);
    }
}
