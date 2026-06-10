using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Oturum geçersizleştirme (auto-logout) çekirdeğini gerçek DB + migration üzerinde doğrular:
/// pasifleştirme/silme/şifre-sıfırlama → SecurityStamp bump → eski cookie damgası artık geçersiz.
/// </summary>
public class SecurityStampValidationIntegrationTests(PostgreSqlFixture pg, WireMockFixture wm)
    : IntegrationTestBase(pg, wm)
{
    private async Task<(Guid Id, string Stamp)> SeedActiveUserAsync()
    {
        using var db = CreateDbContext();
        var id = Guid.NewGuid();
        var stamp = Guid.NewGuid().ToString("N");
        var shortName = id.ToString("N")[..12]; // UserName kolonu varchar(30)
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            Name = "Sess",
            Surname = "Test",
            UserName = $"sess{shortName}",
            NormalizedUserName = $"SESS{shortName.ToUpperInvariant()}",
            IsActive = true,
            SecurityStamp = stamp,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return (id, stamp);
    }

    [Fact]
    public async Task Validator_ActiveUser_WithCurrentStamp_IsValid()
    {
        var (id, stamp) = await SeedActiveUserAsync();
        var (validator, scope) = GetScopedService<ISecurityStampValidator>();
        using (scope)
        {
            var valid = await validator.IsValidAsync(id, stamp);
            valid.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SetPassive_InvalidatesExistingSessionStamp()
    {
        var (id, oldStamp) = await SeedActiveUserAsync();

        var (manager, mScope) = GetScopedService<IApplicationUserManager>();
        using (mScope)
        {
            (await manager.SetPassive(id)).Success.Should().BeTrue();
        }

        // Eski cookie damgası artık geçersiz → sonraki istekte oturum düşer.
        var (validator, vScope) = GetScopedService<ISecurityStampValidator>();
        using (vScope)
        {
            (await validator.IsValidAsync(id, oldStamp)).Should().BeFalse();
        }
    }

    [Fact]
    public async Task AdminResetPassword_InvalidatesExistingSessionStamp()
    {
        var (id, oldStamp) = await SeedActiveUserAsync();

        var (manager, mScope) = GetScopedService<IApplicationUserManager>();
        using (mScope)
        {
            var result = await manager.AdminResetPassword(id);
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
        }

        var (validator, vScope) = GetScopedService<ISecurityStampValidator>();
        using (vScope)
        {
            (await validator.IsValidAsync(id, oldStamp)).Should().BeFalse();
        }
    }

    [Fact]
    public async Task SoftDelete_InvalidatesExistingSessionStamp()
    {
        var (id, oldStamp) = await SeedActiveUserAsync();

        var (manager, mScope) = GetScopedService<IApplicationUserManager>();
        using (mScope)
        {
            (await manager.SoftDelete(id)).Success.Should().BeTrue();
        }

        var (validator, vScope) = GetScopedService<ISecurityStampValidator>();
        using (vScope)
        {
            (await validator.IsValidAsync(id, oldStamp)).Should().BeFalse();
        }
    }
}
