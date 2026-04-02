using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Entegrasyon.E2E.TestData;

/// <summary>
/// E2E testleri için veritabanına test verilerini yazar.
/// HMAC-SHA512 ile şifre hash/salt oluşturur (uygulamanın HashingHelper'ı ile aynı algoritma).
/// </summary>
public static class TestDataSeeder
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.e2e.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    /// <summary>
    /// Test verilerini DB'ye yazar. İdempotent — birden fazla çalıştırılabilir.
    /// </summary>
    public static async Task SeedAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("E2E_DB_CONNECTION")
                               ?? Configuration["E2E:DbConnectionString"]
                               ?? "Host=localhost;Port=5433;Database=IntegrationDb_E2E;Username=e2e_user;Password=e2e_password";

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await SeedBranchOfficeAsync(conn);
        await UpdateAdminUserPasswordAsync(conn);
        await SeedTestCategoriesAsync(conn);
        await SeedTestBrandAsync(conn);
    }

    /// <summary>
    /// Admin kullanıcının şifresini E2E test şifresiyle günceller.
    /// NeedsTakeNewPassword=false yapılarak ilk giriş password reset atlanır.
    /// </summary>
    private static async Task UpdateAdminUserPasswordAsync(NpgsqlConnection conn)
    {
        var (hash, salt) = CreatePasswordHash(TestUsers.AdminPassword);

        await using var cmd = new NpgsqlCommand(@"
            UPDATE ""Users"" SET
                ""PasswordHash"" = @hash,
                ""PasswordSalt"" = @salt,
                ""NeedsTakeNewPassword"" = false,
                ""IsActive"" = true
            WHERE ""Id"" = @id", conn);

        cmd.Parameters.AddWithValue("hash", hash);
        cmd.Parameters.AddWithValue("salt", salt);
        cmd.Parameters.AddWithValue("id", TestUsers.AdminUserId);

        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
            throw new InvalidOperationException(
                "Admin kullanıcı DB'de bulunamadı — migration tamamlanmamış olabilir. " +
                $"Aranan UserId: {TestUsers.AdminUserId}");
    }

    private static async Task SeedBranchOfficeAsync(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO ""BranchOffices"" (""Id"", ""Name"", ""IsDefaultMarketPlaceStock"", ""CreatedAt"", ""UpdatedAt"", ""DeletedAt"", ""IsDeleted"")
            VALUES (1, 'Merkez Şube', true, @now, @now, @epoch, false)
            ON CONFLICT (""Id"") DO NOTHING", conn);

        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("epoch", DateTimeOffset.MinValue);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SeedTestCategoriesAsync(NpgsqlConnection conn)
    {
        // Ana kategori
        await using var cmd1 = new NpgsqlCommand(@"
            INSERT INTO ""Categories"" (""Id"", ""Name"", ""SuperCategoryId"", ""CreatedAt"", ""UpdatedAt"", ""DeletedAt"", ""IsDeleted"")
            VALUES (9900, 'E2E Test Ana Kategori', null, @now, @now, @epoch, false)
            ON CONFLICT (""Id"") DO NOTHING", conn);
        cmd1.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        cmd1.Parameters.AddWithValue("epoch", DateTimeOffset.MinValue);
        await cmd1.ExecuteNonQueryAsync();

        // Alt kategori
        await using var cmd2 = new NpgsqlCommand(@"
            INSERT INTO ""Categories"" (""Id"", ""Name"", ""SuperCategoryId"", ""CreatedAt"", ""UpdatedAt"", ""DeletedAt"", ""IsDeleted"")
            VALUES (9901, 'E2E Test Alt Kategori', 9900, @now, @now, @epoch, false)
            ON CONFLICT (""Id"") DO NOTHING", conn);
        cmd2.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        cmd2.Parameters.AddWithValue("epoch", DateTimeOffset.MinValue);
        await cmd2.ExecuteNonQueryAsync();
    }

    private static async Task SeedTestBrandAsync(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO ""Brands"" (""Id"", ""Name"", ""CreatedAt"", ""UpdatedAt"", ""DeletedAt"", ""IsDeleted"")
            VALUES (9900, 'E2E Test Marka', @now, @now, @epoch, false)
            ON CONFLICT (""Id"") DO NOTHING", conn);

        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("epoch", DateTimeOffset.MinValue);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// HMAC-SHA512 ile şifre hash ve salt oluşturur.
    /// Uygulamanın HashingHelper.CreatePasswordHash ile aynı algoritma.
    /// </summary>
    private static (byte[] hash, byte[] salt) CreatePasswordHash(string password)
    {
        using var hmac = new HMACSHA512();
        var salt = hmac.Key;
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return (hash, salt);
    }
}
