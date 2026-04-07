using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontContactManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontContactManager _sut;

    public StorefrontContactManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontContactManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task SubmitMessageAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontContactMessages).ReturnsDbSet(new List<StorefrontContactMessage>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.SubmitMessageAsync(1, "Ali Veli", "ali@test.com", "555-1234", "Sipariş", "Siparişim hakkinda bilgi istiyorum.");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("basariyla");
    }

    [Fact]
    public async Task SubmitMessageAsync_EmptyName_ReturnsError()
    {
        // Act
        var result = await _sut.SubmitMessageAsync(1, "", "ali@test.com", null, null, "Mesaj");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Ad soyad");
    }

    [Fact]
    public async Task SubmitMessageAsync_EmptyEmail_ReturnsError()
    {
        // Act
        var result = await _sut.SubmitMessageAsync(1, "Ali", "", null, null, "Mesaj");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("E-posta");
    }

    [Fact]
    public async Task SubmitMessageAsync_EmptyMessage_ReturnsError()
    {
        // Act
        var result = await _sut.SubmitMessageAsync(1, "Ali", "ali@test.com", null, null, "");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Mesaj");
    }
}
