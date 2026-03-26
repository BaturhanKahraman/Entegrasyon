using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Storefront;

public class IyzicoPaymentServiceTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly Mock<ILogger<IyzicoPaymentService>> _mockLogger;
    private readonly IyzicoPaymentService _sut;

    public IyzicoPaymentServiceTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);
        _mockLogger = new Mock<ILogger<IyzicoPaymentService>>();

        _sut = new IyzicoPaymentService(_mockContextFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InitiatePaymentAsync_NoConfig_ReturnsError()
    {
        // Arrange — no StorefrontPaymentConfig in DB
        _mockDbContext.Setup(x => x.Set<StorefrontPaymentConfig>())
            .ReturnsDbSet(new List<StorefrontPaymentConfig>());

        var request = new PaymentRequest(
            OrderId: Guid.NewGuid(),
            Amount: 100m,
            CustomerEmail: "test@example.com",
            CustomerName: "Test",
            CustomerSurname: "User",
            CustomerPhone: "+905551234567",
            CustomerIp: "127.0.0.1",
            CustomerCity: "Istanbul",
            CustomerAddress: "Test Address",
            CallbackUrl: "https://localhost/odeme/callback",
            Items: new List<PaymentItemDto>
            {
                new("Test Product", "Genel", 100m, "1")
            });

        // Act
        var result = await _sut.InitiatePaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("iyzico yapilandirilmamis");
    }

    [Fact]
    public async Task HandleCallbackAsync_NoConfig_ReturnsError()
    {
        // Arrange — no StorefrontPaymentConfig in DB
        _mockDbContext.Setup(x => x.Set<StorefrontPaymentConfig>())
            .ReturnsDbSet(new List<StorefrontPaymentConfig>());

        // Act
        var result = await _sut.HandleCallbackAsync("some-token");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("iyzico yapilandirilmamis");
    }

    [Fact]
    public async Task InitiatePaymentAsync_InactiveConfig_ReturnsError()
    {
        // Arrange — config exists but inactive
        var config = new StorefrontPaymentConfig
        {
            Id = 1, TenantId = 1, PaymentProvider = "Iyzico",
            ApiKey = "key", SecretKey = "secret", IsActive = false
        };
        _mockDbContext.Setup(x => x.Set<StorefrontPaymentConfig>())
            .ReturnsDbSet(new List<StorefrontPaymentConfig> { config });

        var request = new PaymentRequest(
            OrderId: Guid.NewGuid(), Amount: 50m,
            CustomerEmail: "test@test.com", CustomerName: "A", CustomerSurname: "B",
            CustomerPhone: "555", CustomerIp: "1.1.1.1",
            CustomerCity: "Ankara", CustomerAddress: "Addr",
            CallbackUrl: "https://localhost/callback",
            Items: new List<PaymentItemDto> { new("Item", "Cat", 50m, "2") });

        // Act
        var result = await _sut.InitiatePaymentAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("iyzico yapilandirilmamis");
    }
}
