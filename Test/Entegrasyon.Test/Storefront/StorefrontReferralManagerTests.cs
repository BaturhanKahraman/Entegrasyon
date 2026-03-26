using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontReferralManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontReferralManager _sut;

    public StorefrontReferralManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontReferralManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task GetOrCreateReferralCodeAsync_NewCustomer_CreatesNewCode()
    {
        // Arrange
        var referrals = new List<StorefrontReferral>();
        _mockDbContext.Setup(x => x.StorefrontReferrals).ReturnsDbSet(referrals);

        // Act
        var result = await _sut.GetOrCreateReferralCodeAsync(1, 42);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveLength(8);
        result.Data.Should().MatchRegex("^[A-Z0-9]+$");
    }

    [Fact]
    public async Task GetOrCreateReferralCodeAsync_ExistingCode_ReturnsExisting()
    {
        // Arrange
        var referrals = new List<StorefrontReferral>
        {
            new()
            {
                Id = 1, TenantId = 1, ReferrerCustomerId = 42,
                ReferralCode = "ABCD1234", Status = ReferralStatus.Pending,
                ReferredCustomerId = null
            }
        };
        _mockDbContext.Setup(x => x.StorefrontReferrals).ReturnsDbSet(referrals);

        // Act
        var result = await _sut.GetOrCreateReferralCodeAsync(1, 42);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("ABCD1234");
    }

    [Fact]
    public async Task RegisterReferralAsync_ValidCode_SetsReferredCustomer()
    {
        // Arrange
        var referral = new StorefrontReferral
        {
            Id = 1, TenantId = 1, ReferrerCustomerId = 42,
            ReferralCode = "ABCD1234", Status = ReferralStatus.Pending,
            ReferredCustomerId = null
        };
        var referrals = new List<StorefrontReferral> { referral };
        _mockDbContext.Setup(x => x.StorefrontReferrals).ReturnsDbSet(referrals);

        // Act
        var result = await _sut.RegisterReferralAsync(1, "ABCD1234", 99);

        // Assert
        result.Success.Should().BeTrue();
        referral.ReferredCustomerId.Should().Be(99);
        referral.Status.Should().Be(ReferralStatus.Registered);
    }

    [Fact]
    public async Task RegisterReferralAsync_SelfReferral_ReturnsError()
    {
        // Arrange
        var referrals = new List<StorefrontReferral>
        {
            new()
            {
                Id = 1, TenantId = 1, ReferrerCustomerId = 42,
                ReferralCode = "ABCD1234", Status = ReferralStatus.Pending,
                ReferredCustomerId = null
            }
        };
        _mockDbContext.Setup(x => x.StorefrontReferrals).ReturnsDbSet(referrals);

        // Act
        var result = await _sut.RegisterReferralAsync(1, "ABCD1234", 42);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Kendi referans");
    }

    [Fact]
    public async Task RegisterReferralAsync_AlreadyReferred_ReturnsError()
    {
        // Arrange
        var referrals = new List<StorefrontReferral>
        {
            new()
            {
                Id = 1, TenantId = 1, ReferrerCustomerId = 42,
                ReferralCode = "ABCD1234", Status = ReferralStatus.Pending,
                ReferredCustomerId = null
            },
            new()
            {
                Id = 2, TenantId = 1, ReferrerCustomerId = 50,
                ReferralCode = "XXXX9999", Status = ReferralStatus.Registered,
                ReferredCustomerId = 99
            }
        };
        _mockDbContext.Setup(x => x.StorefrontReferrals).ReturnsDbSet(referrals);

        // Act
        var result = await _sut.RegisterReferralAsync(1, "ABCD1234", 99);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten");
    }

    [Fact]
    public async Task RegisterReferralAsync_EmptyCode_ReturnsError()
    {
        // Act
        var result = await _sut.RegisterReferralAsync(1, "", 99);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bos");
    }

    [Fact]
    public async Task RegisterReferralAsync_InvalidCode_ReturnsError()
    {
        // Arrange
        var referrals = new List<StorefrontReferral>();
        _mockDbContext.Setup(x => x.StorefrontReferrals).ReturnsDbSet(referrals);

        // Act
        var result = await _sut.RegisterReferralAsync(1, "INVALID1", 99);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task GetReferralsAsync_ReturnsOnlyCompletedReferrals()
    {
        // Arrange
        var referrals = new List<StorefrontReferral>
        {
            new()
            {
                Id = 1, TenantId = 1, ReferrerCustomerId = 42,
                ReferralCode = "ABCD1234", Status = ReferralStatus.Registered,
                ReferredCustomerId = 99
            },
            new()
            {
                Id = 2, TenantId = 1, ReferrerCustomerId = 42,
                ReferralCode = "ABCD1234", Status = ReferralStatus.Pending,
                ReferredCustomerId = null
            }
        };
        _mockDbContext.Setup(x => x.StorefrontReferrals).ReturnsDbSet(referrals);

        // Act
        var result = await _sut.GetReferralsAsync(1, 42);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].ReferredCustomerId.Should().Be(99);
    }
}
