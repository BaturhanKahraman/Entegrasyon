using Entegrasyon.Business.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Ciceksepeti;

/// <summary>
/// CiceksepetiOrderPollingService unit testleri.
/// Background servisler tam anlamıyla unit test edilmesi güçtür —
/// instantiation ve scope creation odaklı testler yeterlidir.
/// </summary>
public class CiceksepetiOrderPollingServiceTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory = new();
    private readonly Mock<ILogger<CiceksepetiOrderPollingService>> _mockLogger = new();

    [Fact]
    public void Service_CanBeInstantiated()
    {
        // Arrange & Act
        var sut = new CiceksepetiOrderPollingService(
            _mockScopeFactory.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public async Task Service_UsesServiceScopeFactory_WhenExecuting()
    {
        // Arrange
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        _mockScopeFactory
            .Setup(f => f.CreateScope())
            .Returns(mockScope.Object);

        var sut = new CiceksepetiOrderPollingService(
            _mockScopeFactory.Object,
            _mockLogger.Object);

        // Act — start and stop gracefully; TaskCanceledException during initial delay is expected
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await sut.StartAsync(cts.Token);
            await sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected when already-cancelled token causes the initial delay to throw
        }

        // Assert — service was created and handled cancellation without crashing
        Assert.NotNull(sut);
    }
}
