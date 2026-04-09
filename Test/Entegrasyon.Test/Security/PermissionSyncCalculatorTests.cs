using Entegrasyon.ApplicationBootstrap.Security;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Security;

/// <summary>
/// Faz 7+: Startup seeder'ın saf (DB'siz) karşılaştırma mantığını doğrular.
/// Beklenen claim listesi ile mevcut claim listesi arasındaki farkı idempotent şekilde hesaplar.
/// </summary>
public class PermissionSyncCalculatorTests
{
    [Fact]
    public void GetMissingPermissions_returns_all_when_existing_is_empty()
    {
        var expected = new[] { "Permissions.A", "Permissions.B", "Permissions.C" };
        var existing = Array.Empty<string>();

        var missing = PermissionSyncCalculator.GetMissingPermissions(existing, expected);

        missing.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GetMissingPermissions_returns_empty_when_all_present()
    {
        var expected = new[] { "Permissions.A", "Permissions.B" };
        var existing = new[] { "Permissions.A", "Permissions.B" };

        var missing = PermissionSyncCalculator.GetMissingPermissions(existing, expected);

        missing.Should().BeEmpty();
    }

    [Fact]
    public void GetMissingPermissions_returns_only_diff_when_partial_match()
    {
        var expected = new[] { "Permissions.A", "Permissions.B", "Permissions.C", "Permissions.D" };
        var existing = new[] { "Permissions.A", "Permissions.C" };

        var missing = PermissionSyncCalculator.GetMissingPermissions(existing, expected);

        missing.Should().BeEquivalentTo(new[] { "Permissions.B", "Permissions.D" });
    }

    [Fact]
    public void GetMissingPermissions_ignores_extra_existing_permissions()
    {
        // Admin'de kod tarafından kaldırılmış eski bir permission varsa silmemeli — sadece eksikleri ekler
        var expected = new[] { "Permissions.A" };
        var existing = new[] { "Permissions.A", "Permissions.Legacy.Obsolete" };

        var missing = PermissionSyncCalculator.GetMissingPermissions(existing, expected);

        missing.Should().BeEmpty();
    }

    [Fact]
    public void GetMissingPermissions_is_case_sensitive()
    {
        // Permission string'leri case-sensitive karşılaştırılmalı (C# string default behavior)
        var expected = new[] { "Permissions.Stock.Transfer" };
        var existing = new[] { "permissions.stock.transfer" };

        var missing = PermissionSyncCalculator.GetMissingPermissions(existing, expected);

        missing.Should().ContainSingle().Which.Should().Be("Permissions.Stock.Transfer");
    }

    [Fact]
    public void GetMissingPermissions_handles_duplicates_in_existing()
    {
        // Legacy DB'de aynı permission iki kez girilmişse crash etmemeli
        var expected = new[] { "Permissions.A", "Permissions.B" };
        var existing = new[] { "Permissions.A", "Permissions.A" };

        var missing = PermissionSyncCalculator.GetMissingPermissions(existing, expected);

        missing.Should().BeEquivalentTo(new[] { "Permissions.B" });
    }

    [Fact]
    public void GetMissingPermissions_with_real_AppPermissions_list_is_deterministic()
    {
        // Smoke test: Gerçek AppPermissions listesi kullanıldığında sıra korunur ve crash yok
        var expected = AppPermissions.GetAllPermissions();
        var existing = expected.Take(expected.Count - 3).ToList();

        var missing = PermissionSyncCalculator.GetMissingPermissions(existing, expected);

        missing.Should().HaveCount(3);
        missing.Should().BeEquivalentTo(expected.TakeLast(3));
    }
}
