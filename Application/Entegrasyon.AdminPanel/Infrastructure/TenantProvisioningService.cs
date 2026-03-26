using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Entegrasyon.AdminPanel.Infrastructure;

public record ProvisionResult(bool Success, string? ErrorMessage = null);

public class TenantProvisioningService(ILogger<TenantProvisioningService> logger)
{
    /// <summary>
    /// Yeni tenant icin PostgreSQL veritabani olusturur ve migration uygular.
    /// </summary>
    public async Task<ProvisionResult> ProvisionAsync(string connectionString, CancellationToken ct = default)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database
            ?? throw new ArgumentException("Connection string must contain a Database name.");

        var adminConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        }.ConnectionString;

        try
        {
            // 1. Veritabani olustur
            await using var adminConnection = new NpgsqlConnection(adminConnectionString);
            await adminConnection.OpenAsync(ct);

            await using var checkCmd = adminConnection.CreateCommand();
            checkCmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
            var exists = await checkCmd.ExecuteScalarAsync(ct);

            if (exists is null)
            {
                await using var createCmd = adminConnection.CreateCommand();
                createCmd.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await createCmd.ExecuteNonQueryAsync(ct);
                logger.LogInformation("Database {DatabaseName} created.", databaseName);
            }
            else
            {
                logger.LogInformation("Database {DatabaseName} already exists.", databaseName);
            }

            // 2. Migration uygula
            var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();
            optionsBuilder.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            await using var dbContext = new IntegrationDbContext(optionsBuilder.Options);
            await dbContext.Database.MigrateAsync(ct);
            logger.LogInformation("Migrations applied to {DatabaseName}.", databaseName);

            return new ProvisionResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision database {DatabaseName}.", databaseName);

            // Rollback: DB olusturulduysa sil
            try
            {
                await using var adminConnection = new NpgsqlConnection(adminConnectionString);
                await adminConnection.OpenAsync(ct);
                await using var dropCmd = adminConnection.CreateCommand();
                dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
                await dropCmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception rollbackEx)
            {
                logger.LogError(rollbackEx, "Failed to rollback database {DatabaseName}.", databaseName);
            }

            return new ProvisionResult(false, ex.Message);
        }
    }
}
