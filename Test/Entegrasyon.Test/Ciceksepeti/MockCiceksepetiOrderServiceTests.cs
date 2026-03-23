using Entegrasyon.Business.Concrete.Ciceksepeti;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

public class MockCiceksepetiOrderServiceTests
{
    private readonly Mock<ILogger<MockCiceksepetiOrderService>> _loggerMock = new();

    private MockCiceksepetiOrderService CreateSut() => new(_loggerMock.Object);

    [Fact]
    public async Task GetOrdersAsync_Should_Return_Success_With_Empty_Orders()
    {
        var sut = CreateSut();
        var request = new CiceksepetiGetOrdersRequest(null, null, 100, 1, null, null, null);
        var result = await sut.GetOrdersAsync(request);
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.SupplierOrderListWithBranch.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadyForCargoWithCsAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiCsCargoRequest([]);
        var result = await sut.ReadyForCargoWithCsAsync(request);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatusWithOwnCargoAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiOwnCargoRequest([]);
        var result = await sut.UpdateStatusWithOwnCargoAsync(request);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeCargoCompanyAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiChangeCargoRequest([]);
        var result = await sut.ChangeCargoCompanyAsync(request);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendCargoMeasurementAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiCargoMeasurementRequest([]);
        var result = await sut.SendCargoMeasurementAsync(request);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendDigitalCodeAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiDigitalCodeRequest([]);
        var result = await sut.SendDigitalCodeAsync(request);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLaborCostAsync_Should_Return_Success()
    {
        var sut = CreateSut();
        var request = new CiceksepetiLaborCostRequest([]);
        var result = await sut.UpdateLaborCostAsync(request);
        result.Success.Should().BeTrue();
    }
}
