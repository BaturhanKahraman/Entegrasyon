using Entegrasyon.Business.Concrete.Ciceksepeti;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

public class MockCiceksepetiReturnServiceTests
{
    private readonly Mock<ILogger<MockCiceksepetiReturnService>> _loggerMock = new();

    private MockCiceksepetiReturnService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task GetReturnOrdersAsync_Should_Return_Success_With_Empty_Items()
    {
        var sut = CreateSut();
        var request = new CiceksepetiGetReturnsRequest(null, null, 100, 1, null);
        var result = await sut.GetReturnOrdersAsync(request);
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.OrderItemList.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmReturnReceivedAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiReturnReceivedRequest([1, 2, 3]);
        var result = await sut.ConfirmReturnReceivedAsync(request);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateReturnAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiReturnEvaluationRequest(123, 1);
        var result = await sut.EvaluateReturnAsync(request);
        result.Success.Should().BeTrue();
    }
}
