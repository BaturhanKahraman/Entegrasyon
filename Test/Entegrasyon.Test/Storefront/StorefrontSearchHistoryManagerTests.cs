using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontSearchHistoryManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontSearchHistoryManager _sut;

    public StorefrontSearchHistoryManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontSearchHistoryManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task RecordSearchAsync_EmptyQuery_ReturnsError()
    {
        // Act
        var result = await _sut.RecordSearchAsync(1, "   ");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task RecordSearchAsync_NewQuery_CreatesRecord()
    {
        // Arrange
        var searches = new List<StorefrontPopularSearch>();
        _mockDbContext.Setup(x => x.StorefrontPopularSearches).ReturnsDbSet(searches);

        // Act
        var result = await _sut.RecordSearchAsync(1, "  Test Query  ");

        // Assert
        result.Success.Should().BeTrue();
        _mockDbContext.Verify(x => x.StorefrontPopularSearches.Add(It.Is<StorefrontPopularSearch>(
            s => s.Query == "test query" && s.SearchCount == 1 && s.TenantId == 1)), Times.Once);
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordSearchAsync_ExistingQuery_IncrementsCount()
    {
        // Arrange
        var existing = new StorefrontPopularSearch
        {
            Id = 1, TenantId = 1, Query = "test query", SearchCount = 5,
            LastSearchedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var searches = new List<StorefrontPopularSearch> { existing };
        _mockDbContext.Setup(x => x.StorefrontPopularSearches).ReturnsDbSet(searches);

        // Act
        var result = await _sut.RecordSearchAsync(1, "Test Query");

        // Assert
        result.Success.Should().BeTrue();
        existing.SearchCount.Should().Be(6);
        _mockDbContext.Verify(x => x.StorefrontPopularSearches.Update(existing), Times.Once);
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPopularSearchesAsync_ReturnsTopSearches()
    {
        // Arrange
        var searches = new List<StorefrontPopularSearch>
        {
            new() { Id = 1, TenantId = 1, Query = "elbise", SearchCount = 100 },
            new() { Id = 2, TenantId = 1, Query = "ayakkabi", SearchCount = 50 },
            new() { Id = 3, TenantId = 1, Query = "canta", SearchCount = 200 },
            new() { Id = 4, TenantId = 2, Query = "other tenant", SearchCount = 999 },
        };
        _mockDbContext.Setup(x => x.StorefrontPopularSearches).ReturnsDbSet(searches);

        // Act
        var result = await _sut.GetPopularSearchesAsync(1, 2);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data[0].Should().Be("canta");
        result.Data[1].Should().Be("elbise");
    }

    [Fact]
    public async Task GetPopularSearchesAsync_EmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var searches = new List<StorefrontPopularSearch>();
        _mockDbContext.Setup(x => x.StorefrontPopularSearches).ReturnsDbSet(searches);

        // Act
        var result = await _sut.GetPopularSearchesAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
