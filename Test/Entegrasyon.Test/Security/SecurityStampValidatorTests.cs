using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Entity.User;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.UnitTest.Security;

/// <summary>
/// Oturum geçersizleştirme (auto-logout) çekirdeği. Bir kullanıcının cookie'sindeki
/// SecurityStamp + IsActive/IsDeleted durumu DB ile tutarlı mı diye doğrular.
/// </summary>
public class SecurityStampValidatorTests : BaseTest
{
    private readonly ISecurityStampValidator validator;

    public SecurityStampValidatorTests()
    {
        validator = new SecurityStampValidator(
            mockContextFactory.Object,
            new Mock<ILogger<SecurityStampValidator>>().Object);
    }

    private static ApplicationUser User(Guid id, string stamp, bool isActive = true, bool isDeleted = false)
        => new()
        {
            Id = id,
            UserName = "test",
            IsActive = isActive,
            IsDeleted = isDeleted,
            SecurityStamp = stamp
        };

    [Fact]
    public async Task IsValidAsync_ActiveUserMatchingStamp_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var user = User(id, "stamp-1");
        mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet([user]);

        var result = await validator.IsValidAsync(id, "stamp-1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidAsync_StampMismatch_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var user = User(id, "new-stamp");
        mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet([user]);

        var result = await validator.IsValidAsync(id, "old-stamp");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_InactiveUser_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var user = User(id, "stamp-1", isActive: false);
        mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet([user]);

        var result = await validator.IsValidAsync(id, "stamp-1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_DeletedUser_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var user = User(id, "stamp-1", isDeleted: true);
        mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet([user]);

        var result = await validator.IsValidAsync(id, "stamp-1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidAsync_UnknownUser_ReturnsFalse()
    {
        mockIntegrationDbContext.Setup(c => c.Users).ReturnsDbSet(new List<ApplicationUser>());

        var result = await validator.IsValidAsync(Guid.NewGuid(), "stamp-1");

        result.Should().BeFalse();
    }
}
