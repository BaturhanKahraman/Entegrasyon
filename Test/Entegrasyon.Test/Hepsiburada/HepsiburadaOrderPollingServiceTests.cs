using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Hepsiburada;

public class HepsiburadaOrderPollingServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _providerMock = new();
    private readonly Mock<IHepsiburadaOrderService> _orderServiceMock = new();
    private readonly Mock<ILogger<HepsiburadaOrderPollingService>> _loggerMock = new();

    public HepsiburadaOrderPollingServiceTests()
    {
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_providerMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _providerMock
            .Setup(x => x.GetService(typeof(IHepsiburadaOrderService)))
            .Returns(_orderServiceMock.Object);
    }

    [Fact]
    public void Constructor_Should_Not_Throw()
    {
        var act = () => new HepsiburadaOrderPollingService(_scopeFactoryMock.Object, _loggerMock.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Cancel_Gracefully()
    {
        _orderServiceMock
            .Setup(x => x.GetOrdersAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new SuccessDataResult<List<HepsiburadaOrderDto>>([]));

        var sut = new HepsiburadaOrderPollingService(_scopeFactoryMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // StartAsync + StopAsync will exercise ExecuteAsync briefly
        await sut.StartAsync(cts.Token);
        await Task.Delay(300);
        await sut.StopAsync(CancellationToken.None);
    }
}
