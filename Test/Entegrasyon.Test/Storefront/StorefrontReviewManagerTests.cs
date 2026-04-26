using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontReviewManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontReviewManager _sut;

    public StorefrontReviewManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontReviewManager(_mockContextFactory.Object, Microsoft.Extensions.Options.Options.Create(new Entegrasyon.Business.FeatureFlags.NotificationFeatureFlags { PublishEnabled = false }));
    }

    [Fact]
    public async Task GetProductReviewsAsync_ReturnsOnlyApprovedReviews()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var reviews = new List<StorefrontReview>
        {
            new() { Id = 1, TenantId = 1, ProductId = productId, Rating = 5, Comment = "Great", IsApproved = true },
            new() { Id = 2, TenantId = 1, ProductId = productId, Rating = 1, Comment = "Bad", IsApproved = false },
            new() { Id = 3, TenantId = 1, ProductId = productId, Rating = 4, Comment = "Good", IsApproved = true },
        };

        _mockDbContext.Setup(x => x.StorefrontReviews).ReturnsDbSet(reviews);

        // Act
        var result = await _sut.GetProductReviewsAsync(1, productId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(r => r.IsApproved);
    }

    [Fact]
    public async Task AddReviewAsync_DuplicateReview_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var reviews = new List<StorefrontReview>
        {
            new() { Id = 1, TenantId = 1, ProductId = productId, CustomerId = 42, Rating = 5, Comment = "Existing" }
        };

        _mockDbContext.Setup(x => x.StorefrontReviews).ReturnsDbSet(reviews);

        // Act
        var result = await _sut.AddReviewAsync(1, productId, 42, 4, "New review", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten");
    }

    [Fact]
    public async Task AddReviewAsync_InvalidRating_ReturnsError()
    {
        // Arrange & Act
        var result = await _sut.AddReviewAsync(1, Guid.NewGuid(), 1, 0, "Comment", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("1-5");
    }

    [Fact]
    public async Task AddReviewAsync_EmptyComment_ReturnsError()
    {
        // Arrange & Act
        var result = await _sut.AddReviewAsync(1, Guid.NewGuid(), 1, 5, "", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task GetProductRatingAsync_CalculatesCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var reviews = new List<StorefrontReview>
        {
            new() { Id = 1, TenantId = 1, ProductId = productId, Rating = 5, Comment = "a", IsApproved = true },
            new() { Id = 2, TenantId = 1, ProductId = productId, Rating = 3, Comment = "b", IsApproved = true },
            new() { Id = 3, TenantId = 1, ProductId = productId, Rating = 1, Comment = "c", IsApproved = false }, // not approved
        };

        _mockDbContext.Setup(x => x.StorefrontReviews).ReturnsDbSet(reviews);

        // Act
        var result = await _sut.GetProductRatingAsync(1, productId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.ReviewCount.Should().Be(2);
        result.Data.AverageRating.Should().Be(4.0);
    }
}
