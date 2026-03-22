using Entegrasyon.Business.Concrete.N11;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.N11;

public class MockN11StockPriceServiceTests
{
    private readonly Mock<ILogger<MockN11StockPriceService>> _loggerMock = new();

    [Fact]
    public async Task UpdatePriceAsync_Should_Return_Success()
    {
        var sut = new MockN11StockPriceService(_loggerMock.Object);
        var result = await sut.UpdatePriceAsync(Guid.NewGuid(), 99.99m);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStockAsync_Should_Return_Success()
    {
        var sut = new MockN11StockPriceService(_loggerMock.Object);
        var result = await sut.UpdateStockAsync(Guid.NewGuid(), 50);
        result.Success.Should().BeTrue();
    }
}
