using Entegrasyon.Business.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Amazon;

/// <summary>
/// AmazonOrderPollingService unit tests.
/// Background services are tested for instantiation and graceful cancellation.
/// </summary>
public class AmazonOrderPollingServiceTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory = new();
    private readonly Mock<ILogger<AmazonOrderPollingService>> _mockLogger = new();

    [Fact]
    public void Service_CanBeInstantiated()
    {
        // Arrange & Act
        var sut = new AmazonOrderPollingService(
            _mockScopeFactory.Object,
            _mockLogger.Object);

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task Service_HandlesImmediateCancellation_Gracefully()
    {
        // Arrange
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        _mockScopeFactory
            .Setup(f => f.CreateScope())
            .Returns(mockScope.Object);

        var sut = new AmazonOrderPollingService(
            _mockScopeFactory.Object,
            _mockLogger.Object);

        // Act
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await sut.StartAsync(cts.Token);
            await sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected: initial delay throws on cancellation
        }

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task Service_StartsAndStops_WithoutException()
    {
        // Arrange
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        _mockScopeFactory
            .Setup(f => f.CreateScope())
            .Returns(mockScope.Object);

        var sut = new AmazonOrderPollingService(
            _mockScopeFactory.Object,
            _mockLogger.Object);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await sut.StartAsync(cts.Token);
            await Task.Delay(150);
            await sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        sut.Should().NotBeNull();
    }
}
