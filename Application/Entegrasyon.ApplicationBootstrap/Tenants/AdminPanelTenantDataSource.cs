using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Entegrasyon.ApplicationBootstrap.Tenants;

/// <summary>
/// AdminPanel SQLite DB'den tenant listesini okur.
/// ITenantRegistryDataSource implementasyonu.
/// appsettings.json'dan ConnectionStrings:AdminPanel kullanir.
/// </summary>
public sealed class AdminPanelTenantDataSource(IConfiguration configuration) : ITenantRegistryDataSource
{
    private static readonly Dictionary<int, string> LicenseTypeMap = new()
    {
        [0] = "Trial",
        [1] = "Standard",
        [2] = "Premium",
        [3] = "Enterprise"
    };

    public async Task<IReadOnlyList<TenantRegistryEntry>> GetAllTenantsAsync()
    {
        var connectionString = configuration.GetConnectionString("AdminPanel")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:AdminPanel is not configured in appsettings.json.");

        var tenants = new List<TenantRegistryEntry>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t."Id", t."Subdomain", t."CompanyName", t."ConnectionString", t."IsActive",
                   (SELECT l."Type" FROM "TenantLicenses" l
                    WHERE l."TenantId" = t."Id"
                      AND NOW() BETWEEN l."StartDate" AND l."EndDate"
                    ORDER BY l."Id" DESC LIMIT 1) as "LicenseType"
            FROM "Tenants" t
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string? licenseType = null;
            if (!reader.IsDBNull(5))
            {
                var rawValue = reader.GetInt32(5);
                LicenseTypeMap.TryGetValue(rawValue, out licenseType);
            }

            tenants.Add(new TenantRegistryEntry(
                TenantId: reader.GetInt32(0),
                Subdomain: reader.GetString(1),
                CompanyName: reader.GetString(2),
                ConnectionString: reader.GetString(3),
                IsActive: reader.GetBoolean(4),
                LicenseType: licenseType));
        }

        return tenants.AsReadOnly();
    }
}
