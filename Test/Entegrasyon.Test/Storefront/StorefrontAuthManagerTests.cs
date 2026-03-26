using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontAuthManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory = new();
    private readonly Mock<IntegrationDbContext> _mockDbContext;

    public StorefrontAuthManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_CreatesAuthAndCustomer()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths).ReturnsDbSet(new List<StorefrontCustomerAuth>());
        _mockDbContext.Setup(x => x.Customers).ReturnsDbSet(new List<Customer>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var dto = new StorefrontRegisterDto(1, "Ali", "Yilmaz", "ali@test.com", "05551234567", "Test1234!", "Test1234!", true, false);

        // Act
        var result = await manager.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Email.Should().Be("ali@test.com");
        result.Data.EmailConfirmed.Should().BeFalse();
        result.Data.EmailConfirmationToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsError()
    {
        // Arrange
        var existing = new StorefrontCustomerAuth
        {
            TenantId = 1, Email = "ali@test.com",
            PasswordHash = new byte[64], PasswordSalt = new byte[128]
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { existing });

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var dto = new StorefrontRegisterDto(1, "Ali", "Yilmaz", "ali@test.com", null, "Test1234!", "Test1234!", true, false);

        // Act
        var result = await manager.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_PasswordMismatch_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths).ReturnsDbSet(new List<StorefrontCustomerAuth>());

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var dto = new StorefrontRegisterDto(1, "Ali", "Yilmaz", "ali@test.com", null, "Test1234!", "Different!", true, false);

        // Act
        var result = await manager.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("esle");
    }

    [Fact]
    public async Task RegisterAsync_KvkkNotConsented_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths).ReturnsDbSet(new List<StorefrontCustomerAuth>());

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var dto = new StorefrontRegisterDto(1, "Ali", "Yilmaz", "ali@test.com", null, "Test1234!", "Test1234!", false, false);

        // Act
        var result = await manager.RegisterAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("KVKK");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuth()
    {
        // Arrange
        Entegrasyon.Business.Utilities.HashingHelper.CreatePasswordHash("Test1234!", out var hash, out var salt);
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 1, Email = "ali@test.com",
            PasswordHash = hash, PasswordSalt = salt, LoginFailedCount = 0
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);

        // Act
        var result = await manager.LoginAsync(1, "ali@test.com", "Test1234!");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Email.Should().Be("ali@test.com");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsError()
    {
        // Arrange
        Entegrasyon.Business.Utilities.HashingHelper.CreatePasswordHash("Test1234!", out var hash, out var salt);
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, Email = "ali@test.com",
            PasswordHash = hash, PasswordSalt = salt, LoginFailedCount = 0
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var result = await manager.LoginAsync(1, "ali@test.com", "WrongPassword!");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ReturnsError()
    {
        // Arrange
        Entegrasyon.Business.Utilities.HashingHelper.CreatePasswordHash("Test1234!", out var hash, out var salt);
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, Email = "ali@test.com",
            PasswordHash = hash, PasswordSalt = salt,
            LoginFailedCount = 5, LockedUntil = DateTimeOffset.UtcNow.AddMinutes(10)
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var result = await manager.LoginAsync(1, "ali@test.com", "Test1234!");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("kilitlen");
    }

    [Fact]
    public async Task LoginAsync_NonExistentEmail_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth>());

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var result = await manager.LoginAsync(1, "nonexistent@test.com", "Test1234!");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmEmailAsync_ValidToken_ConfirmsEmail()
    {
        // Arrange
        var token = Convert.ToBase64String(new byte[32]);
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, Email = "ali@test.com",
            PasswordHash = new byte[64], PasswordSalt = new byte[128],
            EmailConfirmed = false,
            EmailConfirmationToken = token,
            EmailConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(23)
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ConfirmEmailAsync(1, token);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmailAsync_ExpiredToken_ReturnsError()
    {
        // Arrange
        var token = Convert.ToBase64String(new byte[32]);
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, Email = "ali@test.com",
            PasswordHash = new byte[64], PasswordSalt = new byte[128],
            EmailConfirmationToken = token,
            EmailConfirmationTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var result = await manager.ConfirmEmailAsync(1, token);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrentPassword_ChangesPassword()
    {
        // Arrange
        Entegrasyon.Business.Utilities.HashingHelper.CreatePasswordHash("OldPass1!", out var hash, out var salt);
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, Email = "ali@test.com",
            PasswordHash = hash, PasswordSalt = salt
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ChangePasswordAsync(1, "OldPass1!", "NewPass1!");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsError()
    {
        // Arrange
        Entegrasyon.Business.Utilities.HashingHelper.CreatePasswordHash("OldPass1!", out var hash, out var salt);
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, Email = "ali@test.com",
            PasswordHash = hash, PasswordSalt = salt
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var result = await manager.ChangePasswordAsync(1, "WrongPass!", "NewPass1!");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RequestPasswordResetAsync_ExistingEmail_ReturnsToken()
    {
        // Arrange
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, Email = "ali@test.com",
            PasswordHash = new byte[64], PasswordSalt = new byte[128]
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);

        // Act
        var result = await manager.RequestPasswordResetAsync(1, "ali@test.com");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ResetsPassword()
    {
        // Arrange
        var token = Convert.ToBase64String(new byte[32]);
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, Email = "ali@test.com",
            PasswordHash = new byte[64], PasswordSalt = new byte[128],
            PasswordResetToken = token,
            PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ResetPasswordAsync(1, token, "NewPassword1!");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetAuthByCustomerIdAsync_ExistingCustomer_ReturnsAuth()
    {
        // Arrange
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 42, Email = "ali@test.com",
            PasswordHash = new byte[64], PasswordSalt = new byte[128]
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);

        // Act
        var result = await manager.GetAuthByCustomerIdAsync(1, 42);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.CustomerId.Should().Be(42);
    }

    [Fact]
    public async Task UpdateProfileAsync_ExistingCustomer_UpdatesFields()
    {
        // Arrange
        var customer = new RetailCustomer
        {
            Id = 1, Name = "Ali", Surname = "Yilmaz",
            PhoneNumber = "555", CustomerType = "Retail"
        };
        _mockDbContext.Setup(x => x.Customers)
            .ReturnsDbSet(new List<Customer> { customer });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);
        var dto = new StorefrontProfileDto("Veli", "Demir", "05559999999");

        // Act
        var result = await manager.UpdateProfileAsync(1, dto);

        // Assert
        result.Success.Should().BeTrue();
    }
}
