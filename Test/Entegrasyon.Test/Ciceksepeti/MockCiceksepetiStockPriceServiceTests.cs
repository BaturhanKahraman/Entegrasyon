using Entegrasyon.Business.Concrete.Ciceksepeti;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

public class MockCiceksepetiStockPriceServiceTests
{
    private readonly Mock<ILogger<MockCiceksepetiStockPriceService>> _loggerMock = new();

    private MockCiceksepetiStockPriceService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task UpdateStockAndPriceAsync_Should_Return_Success_With_BatchIds()
    {
        var sut = CreateSut();
        var items = new List<CiceksepetiStockPriceItem>
        {
            new("SKU1", 10, 99.90m, 119.90m),
            new("SKU2", 5, 49.90m, 59.90m)
        };
        var result = await sut.UpdateStockAndPriceAsync(items);
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
    }
}
