using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontPageManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontPageManager _sut;

    public StorefrontPageManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontPageManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task GetBySlugAsync_ExistingPublishedPage_ReturnsSuccess()
    {
        // Arrange
        var page = new StorefrontPage
        {
            Id = 1, TenantId = 10, Title = "About Us", Slug = "hakkimizda",
            ContentHtml = "<p>About</p>", IsPublished = true, DisplayOrder = 1
        };

        _mockDbContext.Setup(x => x.StorefrontPages)
            .ReturnsDbSet(new List<StorefrontPage> { page });

        // Act
        var result = await _sut.GetBySlugAsync(10, "hakkimizda");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Title.Should().Be("About Us");
    }

    [Fact]
    public async Task GetBySlugAsync_NonexistentSlug_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontPages)
            .ReturnsDbSet(new List<StorefrontPage>());

        // Act
        var result = await _sut.GetBySlugAsync(10, "nonexistent");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetPublishedPagesAsync_FiltersCorrectly()
    {
        // Arrange
        var pages = new List<StorefrontPage>
        {
            new() { Id = 1, TenantId = 10, Title = "Page 1", Slug = "page-1",
                ContentHtml = "<p>1</p>", IsPublished = true, DisplayOrder = 2 },
            new() { Id = 2, TenantId = 10, Title = "Page 2", Slug = "page-2",
                ContentHtml = "<p>2</p>", IsPublished = false, DisplayOrder = 1 }, // unpublished
            new() { Id = 3, TenantId = 20, Title = "Page 3", Slug = "page-3",
                ContentHtml = "<p>3</p>", IsPublished = true, DisplayOrder = 1 }, // different tenant
            new() { Id = 4, TenantId = 10, Title = "Page 4", Slug = "page-4",
                ContentHtml = "<p>4</p>", IsPublished = true, DisplayOrder = 1 },
        };

        _mockDbContext.Setup(x => x.StorefrontPages)
            .ReturnsDbSet(pages);

        // Act
        var result = await _sut.GetPublishedPagesAsync(10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2); // only published + tenant 10
        result.Data[0].Title.Should().Be("Page 4"); // DisplayOrder=1 first
        result.Data[1].Title.Should().Be("Page 1"); // DisplayOrder=2 second
    }
}
