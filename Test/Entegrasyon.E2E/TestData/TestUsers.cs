namespace Entegrasyon.E2E.TestData;

/// <summary>
/// E2E testleri için test kullanıcı bilgileri.
/// Docker ortamında TestDataSeeder tarafından DB'ye yazılır.
/// Local debug'da environment variable ile override edilebilir:
///   E2E_ADMIN_USERNAME=Admin E2E_ADMIN_PASSWORD=SifreXyz dotnet test ...
/// </summary>
public static class TestUsers
{
    public static string AdminUsername =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_USERNAME") ?? "Admin";

    public static string AdminPassword =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "123456789";

    public static readonly Guid AdminUserId = new("DFDA5D4A-F807-408C-9B4D-908830AD5724");
}
