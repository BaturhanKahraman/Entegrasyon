using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontSizeGuideManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontSizeGuideManager _sut;

    public StorefrontSizeGuideManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontSizeGuideManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task GetSizeGuideForCategoryAsync_MatchingCategory_ReturnsGuide()
    {
        // Arrange
        var guides = new List<StorefrontSizeGuide>
        {
            new() { Id = 1, TenantId = 1, Name = "Erkek Tisort", CategoryIds = "[10, 20, 30]", SizeData = "[]", IsActive = true },
            new() { Id = 2, TenantId = 1, Name = "Kadin Elbise", CategoryIds = "[40, 50]", SizeData = "[]", IsActive = true }
        };

        _mockDbContext.Setup(x => x.StorefrontSizeGuides).ReturnsDbSet(guides);

        // Act
        var result = await _sut.GetSizeGuideForCategoryAsync(1, 20);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Erkek Tisort");
    }

    [Fact]
    public async Task GetSizeGuideForCategoryAsync_NoMatch_ReturnsNull()
    {
        // Arrange
        var guides = new List<StorefrontSizeGuide>
        {
            new() { Id = 1, TenantId = 1, Name = "Test", CategoryIds = "[10]", SizeData = "[]", IsActive = true }
        };

        _mockDbContext.Setup(x => x.StorefrontSizeGuides).ReturnsDbSet(guides);

        // Act
        var result = await _sut.GetSizeGuideForCategoryAsync(1, 999);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetSizeGuideForCategoryAsync_InactiveGuide_NotReturned()
    {
        // Arrange
        var guides = new List<StorefrontSizeGuide>
        {
            new() { Id = 1, TenantId = 1, Name = "Test", CategoryIds = "[10]", SizeData = "[]", IsActive = false }
        };

        _mockDbContext.Setup(x => x.StorefrontSizeGuides).ReturnsDbSet(guides);

        // Act
        var result = await _sut.GetSizeGuideForCategoryAsync(1, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_NewGuide_ReturnsSuccess()
    {
        // Arrange
        var guide = new StorefrontSizeGuide
        {
            TenantId = 1,
            Name = "Yeni Rehber",
            SizeData = "[[\"Beden\",\"Gogus\"],[\"S\",\"90\"]]"
        };

        _mockDbContext.Setup(x => x.StorefrontSizeGuides).ReturnsDbSet(new List<StorefrontSizeGuide>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.CreateOrUpdateAsync(guide);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("basariyla");
    }

    [Fact]
    public async Task CreateOrUpdateAsync_EmptyName_ReturnsError()
    {
        // Arrange
        var guide = new StorefrontSizeGuide { Name = "", SizeData = "[]" };

        // Act
        var result = await _sut.CreateOrUpdateAsync(guide);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("adi");
    }

    [Fact]
    public async Task CreateOrUpdateAsync_EmptySizeData_ReturnsError()
    {
        // Arrange
        var guide = new StorefrontSizeGuide { Name = "Test", SizeData = "" };

        // Act
        var result = await _sut.CreateOrUpdateAsync(guide);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("verileri");
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ExistingGuide_UpdatesSuccessfully()
    {
        // Arrange
        var existingGuide = new StorefrontSizeGuide
        {
            Id = 5, TenantId = 1, Name = "Eski", SizeData = "[]", IsActive = true
        };

        _mockDbContext.Setup(x => x.StorefrontSizeGuides).ReturnsDbSet(new List<StorefrontSizeGuide> { existingGuide });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var updateGuide = new StorefrontSizeGuide
        {
            Id = 5, TenantId = 1, Name = "Guncellenmis", SizeData = "[[\"S\",\"90\"]]", IsActive = true
        };

        // Act
        var result = await _sut.CreateOrUpdateAsync(updateGuide);

        // Assert
        result.Success.Should().BeTrue();
        existingGuide.Name.Should().Be("Guncellenmis");
    }

    [Fact]
    public async Task DeleteAsync_ExistingGuide_SoftDeletes()
    {
        // Arrange
        var guide = new StorefrontSizeGuide { Id = 1, TenantId = 1, Name = "Test", SizeData = "[]" };
        _mockDbContext.Setup(x => x.StorefrontSizeGuides).ReturnsDbSet(new List<StorefrontSizeGuide> { guide });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        guide.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonexistentGuide_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontSizeGuides).ReturnsDbSet(new List<StorefrontSizeGuide>());

        // Act
        var result = await _sut.DeleteAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task GetAllSizeGuidesAsync_ReturnsAllForTenant()
    {
        // Arrange
        var guides = new List<StorefrontSizeGuide>
        {
            new() { Id = 1, TenantId = 1, Name = "A", SizeData = "[]", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
            new() { Id = 2, TenantId = 1, Name = "B", SizeData = "[]", CreatedAt = DateTimeOffset.UtcNow },
            new() { Id = 3, TenantId = 2, Name = "C", SizeData = "[]", CreatedAt = DateTimeOffset.UtcNow }
        };

        _mockDbContext.Setup(x => x.StorefrontSizeGuides).ReturnsDbSet(guides);

        // Act
        var result = await _sut.GetAllSizeGuidesAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }
}
