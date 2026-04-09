namespace Entegrasyon.ApplicationBootstrap.Security;

/// <summary>
/// Startup permission seeder için saf (DB'siz) karşılaştırma yardımcısı.
/// Admin role claim'lerini kod tarafındaki <see cref="AppPermissions.GetAllPermissions"/> listesiyle
/// senkronize tutmak için idempotent fark hesaplama sağlar.
///
/// Tasarım notu: Bu sınıf kasıtlı olarak DbContext'e bağımlı değil — "Functional Core, Imperative Shell"
/// pattern'i ile saf fonksiyon olarak tutulur, böylece DB altyapısı olmadan unit test edilebilir.
/// </summary>
public static class PermissionSyncCalculator
{
    /// <summary>
    /// Beklenen permission listesinden mevcut listede olmayanları döner.
    /// Fazla olanlar (<paramref name="existingPermissions"/>'ta olup <paramref name="expectedPermissions"/>'ta olmayan)
    /// KORUNUR — bu fonksiyon sadece eklenecekleri hesaplar, silme yapmaz.
    /// Karşılaştırma case-sensitive ve ordinal'dir.
    /// </summary>
    /// <param name="existingPermissions">Admin role'ün şu an DB'deki claim'leri (duplicate'lar tolere edilir).</param>
    /// <param name="expectedPermissions">Kod tarafında beklenen claim listesi.</param>
    /// <returns>Eklenmesi gereken claim'lerin sırası korunmuş listesi (expected sırasına göre).</returns>
    public static IReadOnlyList<string> GetMissingPermissions(
        IEnumerable<string> existingPermissions,
        IEnumerable<string> expectedPermissions)
    {
        var existingSet = new HashSet<string>(existingPermissions, StringComparer.Ordinal);
        return expectedPermissions
            .Where(p => !existingSet.Contains(p))
            .ToList();
    }
}
