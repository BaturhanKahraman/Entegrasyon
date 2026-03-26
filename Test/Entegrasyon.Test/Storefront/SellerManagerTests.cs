using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class SellerManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory = new();
    private readonly Mock<IntegrationDbContext> _mockDbContext;

    public SellerManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);
    }

    [Fact]
    public async Task RegisterSellerAsync_ValidData_CreatesSeller()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Sellers).ReturnsDbSet(new List<Seller>());
        _mockDbContext.Setup(x => x.SellerBalances).ReturnsDbSet(new List<SellerBalance>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerManager(_mockContextFactory.Object);
        var dto = new SellerRegistrationDto(
            "Test Magaza", "Aciklama",
            "Test Sirket", "1234567890", "Kadikoy",
            "TR123456789012345678901234", "05551234567", "test@test.com",
            "Test Adres", "Istanbul");

        // Act
        var result = await manager.RegisterSellerAsync(1, 42, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.StoreName.Should().Be("Test Magaza");
        result.Data.StoreSlug.Should().Be("test-magaza");
        result.Data.Status.Should().Be(SellerStatus.Pending);
        result.Data.CustomerId.Should().Be(42);
        result.Data.TenantId.Should().Be(1);
    }

    [Fact]
    public async Task RegisterSellerAsync_AlreadySeller_ReturnsError()
    {
        // Arrange
        var existing = new Seller
        {
            TenantId = 1, CustomerId = 42, StoreName = "Existing",
            CompanyName = "Co", TaxNumber = "123", TaxOffice = "Off",
            ContactPhone = "555", ContactEmail = "e@e.com",
            Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.Sellers).ReturnsDbSet(new List<Seller> { existing });

        var manager = new SellerManager(_mockContextFactory.Object);
        var dto = new SellerRegistrationDto(
            "New Store", null, "Co", "123", "Off",
            null, "555", "e@e.com", "A", "C");

        // Act
        var result = await manager.RegisterSellerAsync(1, 42, dto);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterSellerAsync_SetsStatusToPending()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Sellers).ReturnsDbSet(new List<Seller>());
        _mockDbContext.Setup(x => x.SellerBalances).ReturnsDbSet(new List<SellerBalance>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerManager(_mockContextFactory.Object);
        var dto = new SellerRegistrationDto(
            "Magaza", null, "Co", "123", "Off",
            null, "555", "e@e.com", "A", "C");

        // Act
        var result = await manager.RegisterSellerAsync(1, 1, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Status.Should().Be(SellerStatus.Pending);
    }

    [Fact]
    public async Task ApproveSellerAsync_ValidSeller_Approves()
    {
        // Arrange
        var seller = new Seller
        {
            Id = 1, Status = SellerStatus.Pending, StoreName = "S",
            CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync(seller);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ApproveSellerAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        seller.Status.Should().Be(SellerStatus.Approved);
        seller.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveSellerAsync_AlreadyApproved_ReturnsError()
    {
        // Arrange
        var seller = new Seller
        {
            Id = 1, Status = SellerStatus.Approved, StoreName = "S",
            CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync(seller);

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ApproveSellerAsync(1);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveSellerAsync_NotFound_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync((Seller?)null);

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ApproveSellerAsync(1);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RejectSellerAsync_ValidSeller_Rejects()
    {
        // Arrange
        var seller = new Seller
        {
            Id = 1, Status = SellerStatus.Pending, StoreName = "S",
            CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync(seller);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.RejectSellerAsync(1, "Eksik belge");

        // Assert
        result.Success.Should().BeTrue();
        seller.Status.Should().Be(SellerStatus.Rejected);
        seller.RejectionReason.Should().Be("Eksik belge");
    }

    [Fact]
    public async Task RejectSellerAsync_NotFound_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync((Seller?)null);

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.RejectSellerAsync(1, "reason");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SuspendSellerAsync_ApprovedSeller_Suspends()
    {
        // Arrange
        var seller = new Seller
        {
            Id = 1, Status = SellerStatus.Approved, StoreName = "S",
            CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync(seller);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.SuspendSellerAsync(1, "Kural ihlali");

        // Assert
        result.Success.Should().BeTrue();
        seller.Status.Should().Be(SellerStatus.Suspended);
    }

    [Fact]
    public async Task SuspendSellerAsync_PendingSeller_ReturnsError()
    {
        // Arrange
        var seller = new Seller
        {
            Id = 1, Status = SellerStatus.Pending, StoreName = "S",
            CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync(seller);

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.SuspendSellerAsync(1, "reason");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetSellerBySlugAsync_Found_ReturnsSeller()
    {
        // Arrange
        var seller = new Seller
        {
            TenantId = 1, StoreSlug = "test-magaza", StoreName = "Test Magaza",
            CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C",
            Customer = new RetailCustomer { Name = "Ali", Surname = "Yilmaz", FullName = "Ali Yilmaz", Address = new Address() }
        };
        _mockDbContext.Setup(x => x.Sellers).ReturnsDbSet(new List<Seller> { seller });

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.GetSellerBySlugAsync(1, "test-magaza");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.StoreName.Should().Be("Test Magaza");
    }

    [Fact]
    public async Task GetSellerBySlugAsync_NotFound_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Sellers).ReturnsDbSet(new List<Seller>());

        var manager = new SellerManager(_mockContextFactory.Object);

        // Act
        var result = await manager.GetSellerBySlugAsync(1, "nonexistent");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSellerProfileAsync_ValidSeller_Updates()
    {
        // Arrange
        var seller = new Seller
        {
            Id = 1, StoreName = "Old",
            CompanyName = "C", TaxNumber = "T", TaxOffice = "O",
            ContactPhone = "P", ContactEmail = "E", Address = "A", City = "C"
        };
        _mockDbContext.Setup(x => x.Sellers.FindAsync(1)).ReturnsAsync(seller);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new SellerManager(_mockContextFactory.Object);
        var dto = new SellerProfileDto("New Name", "Desc", null, "555", "new@e.com", "New Addr", "Ankara", null);

        // Act
        var result = await manager.UpdateSellerProfileAsync(1, dto);

        // Assert
        result.Success.Should().BeTrue();
        seller.StoreName.Should().Be("New Name");
        seller.City.Should().Be("Ankara");
    }
}
