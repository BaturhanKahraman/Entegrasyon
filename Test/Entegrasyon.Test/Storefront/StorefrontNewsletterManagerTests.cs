using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontNewsletterManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontNewsletterManager _sut;

    public StorefrontNewsletterManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontNewsletterManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task SubscribeAsync_NewEmail_ReturnsSuccess()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontNewsletters).ReturnsDbSet(new List<StorefrontNewsletter>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.SubscribeAsync(1, "test@example.com", "Test User");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("basariyla");
    }

    [Fact]
    public async Task SubscribeAsync_DuplicateActiveEmail_ReturnsError()
    {
        // Arrange
        var existing = new List<StorefrontNewsletter>
        {
            new() { Id = 1, TenantId = 1, Email = "test@example.com", IsActive = true }
        };

        _mockDbContext.Setup(x => x.StorefrontNewsletters).ReturnsDbSet(existing);

        // Act
        var result = await _sut.SubscribeAsync(1, "test@example.com", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten");
    }

    [Fact]
    public async Task SubscribeAsync_ReactivateInactive_ReturnsSuccess()
    {
        // Arrange
        var existing = new List<StorefrontNewsletter>
        {
            new() { Id = 1, TenantId = 1, Email = "test@example.com", IsActive = false }
        };

        _mockDbContext.Setup(x => x.StorefrontNewsletters).ReturnsDbSet(existing);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.SubscribeAsync(1, "test@example.com", null);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("aktif");
    }

    [Fact]
    public async Task UnsubscribeAsync_ExistingEmail_ReturnsSuccess()
    {
        // Arrange
        var existing = new List<StorefrontNewsletter>
        {
            new() { Id = 1, TenantId = 1, Email = "test@example.com", IsActive = true }
        };

        _mockDbContext.Setup(x => x.StorefrontNewsletters).ReturnsDbSet(existing);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.UnsubscribeAsync(1, "test@example.com");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UnsubscribeAsync_NonexistentEmail_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontNewsletters).ReturnsDbSet(new List<StorefrontNewsletter>());

        // Act
        var result = await _sut.UnsubscribeAsync(1, "nonexistent@example.com");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task SubscribeAsync_EmptyEmail_ReturnsError()
    {
        // Act
        var result = await _sut.SubscribeAsync(1, "", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("E-posta");
    }
}
