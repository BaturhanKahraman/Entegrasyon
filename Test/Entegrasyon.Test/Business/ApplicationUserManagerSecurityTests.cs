using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.User;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// Güvenlik-kritik kullanıcı işlemleri: pasifleştir/sil/şifre-sıfırla → SecurityStamp bump
/// (aktif oturumu düşürür) + admin şifre sıfırlama akışı.
/// </summary>
public class ApplicationUserManagerSecurityTests : BaseTest
{
    private readonly IApplicationUserManager manager;

    public ApplicationUserManagerSecurityTests()
    {
        var validator = new Mock<IFluentValidator>();
        manager = new ApplicationUserManager(
            new UserMapper(),
            mockApplicationLogger.Object,
            new Mock<IHttpContextAccessor>().Object,
            validator.Object,
            mockContextFactory.Object,
            new Mock<ILogger<ApplicationUserManager>>().Object);
    }

    private static ApplicationUser ActiveUser(Guid id, string stamp = "initial")
        => new()
        {
            Id = id,
            UserName = "test",
            IsActive = true,
            SecurityStamp = stamp
        };

    [Fact]
    public async Task SetPassive_BumpsSecurityStamp_AndDeactivates()
    {
        var id = Guid.NewGuid();
        var user = ActiveUser(id);
        mockIntegrationDbContext.Setup(c => c.Users.FindAsync(id)).ReturnsAsync(user);

        var result = await manager.SetPassive(id);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.SecurityStamp.Should().NotBe("initial");
        user.SecurityStamp.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ToggleActive_WhenDeactivating_BumpsSecurityStamp()
    {
        var id = Guid.NewGuid();
        var user = ActiveUser(id); // active → toggling makes passive
        mockIntegrationDbContext.Setup(c => c.Users.FindAsync(id)).ReturnsAsync(user);

        var result = await manager.ToggleActive(id);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.SecurityStamp.Should().NotBe("initial");
    }

    [Fact]
    public async Task ToggleActive_WhenReactivating_DoesNotNeedBump()
    {
        var id = Guid.NewGuid();
        var user = ActiveUser(id, "initial");
        user.IsActive = false; // passive → toggling makes active
        mockIntegrationDbContext.Setup(c => c.Users.FindAsync(id)).ReturnsAsync(user);

        var result = await manager.ToggleActive(id);

        result.Success.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        // Reaktivasyonda mevcut oturumu düşürmeye gerek yok; stamp aynen kalabilir.
        user.SecurityStamp.Should().Be("initial");
    }

    [Fact]
    public async Task SoftDelete_BumpsSecurityStamp_AndMarksDeleted()
    {
        var id = Guid.NewGuid();
        var user = ActiveUser(id);
        mockIntegrationDbContext.Setup(c => c.Users.FindAsync(
            It.Is<object[]>(o => (Guid)o[0] == id), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await manager.SoftDelete(id);

        result.Success.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        user.SecurityStamp.Should().NotBe("initial");
    }

    [Fact]
    public async Task AdminResetPassword_AssignsTempPassword_SetsNeedsNewPassword_BumpsStamp()
    {
        var id = Guid.NewGuid();
        var user = ActiveUser(id);
        mockIntegrationDbContext.Setup(c => c.Users.FindAsync(
            It.Is<object[]>(o => (Guid)o[0] == id), It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await manager.AdminResetPassword(id);

        result.Should().BeOfType<SuccessDataResult<string>>();
        result.Success.Should().BeTrue();
        user.NeedsTakeNewPassword.Should().BeTrue();
        user.TemporaryPassword.Should().NotBeNullOrEmpty();
        user.SecurityStamp.Should().NotBe("initial");
        // Üretilen geçici şifre kullanıcıya gösterilmek üzere dönülür.
        result.As<SuccessDataResult<string>>().Data.Should().Be(user.TemporaryPassword);
    }

    [Fact]
    public async Task AdminResetPassword_UnknownUser_ReturnsError()
    {
        var id = Guid.NewGuid();
        mockIntegrationDbContext.Setup(c => c.Users.FindAsync(
            It.IsAny<object[]>(), It.IsAny<CancellationToken>())).ReturnsAsync((ApplicationUser?)null);

        var result = await manager.AdminResetPassword(id);

        result.Success.Should().BeFalse();
    }
}
