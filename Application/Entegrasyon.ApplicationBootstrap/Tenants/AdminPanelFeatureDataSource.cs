using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Entegrasyon.ApplicationBootstrap.Tenants;

/// <summary>
/// AdminPanel SQLite DB'den tenant'in aktif subscription permission'larini okur.
/// IMemoryCache ile 5dk cache'ler.
/// </summary>
public sealed class AdminPanelFeatureDataSource(
    IConfiguration configuration,
    IMemoryCache cache) : IFeatureDataSource
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlySet<string>> GetTenantFeaturesAsync(int tenantId)
    {
        var cacheKey = $"tenant:features:{tenantId}";
        if (cache.TryGetValue(cacheKey, out IReadOnlySet<string>? cached) && cached is not null)
            return cached;

        var connectionString = configuration.GetConnectionString("AdminPanel")
            ?? throw new InvalidOperationException("ConnectionStrings:AdminPanel is not configured.");

        var permissions = new HashSet<string>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT fpp."PermissionKey"
            FROM "TenantSubscriptions" ts
            INNER JOIN "FeaturePackages" fp ON fp."Id" = ts."FeaturePackageId"
            INNER JOIN "FeaturePackagePermissions" fpp ON fpp."FeaturePackageId" = fp."Id"
            WHERE ts."TenantId" = @tenantId
              AND ts."IsActive" = TRUE
              AND (ts."EndDate" IS NULL OR ts."EndDate" >= NOW())
            """;
        command.Parameters.AddWithValue("@tenantId", tenantId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            permissions.Add(reader.GetString(0));
        }

        IReadOnlySet<string> result = permissions;
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }
}
