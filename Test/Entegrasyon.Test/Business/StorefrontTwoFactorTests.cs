using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.Entity.Storefront;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class StorefrontTwoFactorTests : BaseTest
{
    private readonly StorefrontAuthManager _sut;

    public StorefrontTwoFactorTests()
    {
        _sut = new StorefrontAuthManager(mockContextFactory.Object, Microsoft.Extensions.Options.Options.Create(new Entegrasyon.Business.FeatureFlags.NotificationFeatureFlags { PublishEnabled = false }));
    }

    [Fact]
    public async Task Enable2FAAsync_WithValidAuth_ReturnsOtpauthUri()
    {
        // Arrange
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 10,
            Email = "test@test.com",
            PasswordHash = new byte[32], PasswordSalt = new byte[16]
        };
        mockIntegrationDbContext.Setup(c => c.StorefrontCustomerAuths.FindAsync(1))
            .ReturnsAsync(auth);

        // Act
        var result = await _sut.Enable2FAAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().StartWith("otpauth://totp/");
        result.Data.Should().Contain("test%40test.com");
        auth.TwoFactorSecret.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Enable2FAAsync_WithNonExistentAuth_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(c => c.StorefrontCustomerAuths.FindAsync(999))
            .ReturnsAsync((StorefrontCustomerAuth?)null);

        // Act
        var result = await _sut.Enable2FAAsync(999);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Verify2FAAsync_WithEmptyCode_ReturnsError()
    {
        // Act
        var result = await _sut.Verify2FAAsync(1, "");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task Verify2FAAsync_WithNoSecret_ReturnsError()
    {
        // Arrange
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 10,
            Email = "test@test.com", TwoFactorSecret = null,
            PasswordHash = new byte[32], PasswordSalt = new byte[16]
        };
        mockIntegrationDbContext.Setup(c => c.StorefrontCustomerAuths.FindAsync(1))
            .ReturnsAsync(auth);

        // Act
        var result = await _sut.Verify2FAAsync(1, "123456");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("yapilandirilmamis");
    }

    [Fact]
    public async Task Verify2FAAsync_WithValidTotpCode_ReturnsSuccess()
    {
        // Arrange
        var secret = OtpNet.Base32Encoding.ToString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(20));
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 10,
            Email = "test@test.com", TwoFactorSecret = secret,
            PasswordHash = new byte[32], PasswordSalt = new byte[16]
        };
        mockIntegrationDbContext.Setup(c => c.StorefrontCustomerAuths.FindAsync(1))
            .ReturnsAsync(auth);

        // Generate valid TOTP code
        var secretBytes = OtpNet.Base32Encoding.ToBytes(secret);
        var totp = new OtpNet.Totp(secretBytes, step: 30, totpSize: 6);
        var validCode = totp.ComputeTotp();

        // Act
        var result = await _sut.Verify2FAAsync(1, validCode);

        // Assert
        result.Success.Should().BeTrue();
        auth.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Verify2FAAsync_WithInvalidCode_ReturnsError()
    {
        // Arrange
        var secret = OtpNet.Base32Encoding.ToString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(20));
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 10,
            Email = "test@test.com", TwoFactorSecret = secret,
            PasswordHash = new byte[32], PasswordSalt = new byte[16]
        };
        mockIntegrationDbContext.Setup(c => c.StorefrontCustomerAuths.FindAsync(1))
            .ReturnsAsync(auth);

        // Act
        var result = await _sut.Verify2FAAsync(1, "000000");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Gecersiz");
    }

    [Fact]
    public async Task GenerateRecoveryCodesAsync_Returns10Codes()
    {
        // Arrange
        IList<StorefrontTwoFactorRecoveryCode> codes = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontTwoFactorRecoveryCodes).ReturnsDbSet(codes);

        // Act
        var result = await _sut.GenerateRecoveryCodesAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(10);
        result.Data.Should().AllSatisfy(c => c.Should().Contain("-"));
    }

    [Fact]
    public async Task VerifyRecoveryCodeAsync_WithEmptyCode_ReturnsError()
    {
        // Act
        var result = await _sut.VerifyRecoveryCodeAsync(1, "");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyRecoveryCodeAsync_WithInvalidCode_ReturnsError()
    {
        // Arrange
        IList<StorefrontTwoFactorRecoveryCode> codes = [];
        mockIntegrationDbContext.Setup(c => c.StorefrontTwoFactorRecoveryCodes).ReturnsDbSet(codes);

        // Act
        var result = await _sut.VerifyRecoveryCodeAsync(1, "invalid-code");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Gecersiz");
    }

    [Fact]
    public async Task IsTwoFactorEnabledAsync_ReturnsFalse_WhenNotEnabled()
    {
        // Arrange
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 10,
            Email = "test@test.com", TwoFactorEnabled = false,
            PasswordHash = new byte[32], PasswordSalt = new byte[16]
        };
        mockIntegrationDbContext.Setup(c => c.StorefrontCustomerAuths.FindAsync(1))
            .ReturnsAsync(auth);

        // Act
        var result = await _sut.IsTwoFactorEnabledAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task IsTwoFactorEnabledAsync_ReturnsTrue_WhenEnabled()
    {
        // Arrange
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 10,
            Email = "test@test.com", TwoFactorEnabled = true,
            PasswordHash = new byte[32], PasswordSalt = new byte[16]
        };
        mockIntegrationDbContext.Setup(c => c.StorefrontCustomerAuths.FindAsync(1))
            .ReturnsAsync(auth);

        // Act
        var result = await _sut.IsTwoFactorEnabledAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }
}
