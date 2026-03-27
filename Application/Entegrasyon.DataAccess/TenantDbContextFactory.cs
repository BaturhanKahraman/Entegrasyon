using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.DataAccess;

/// <summary>
/// Scoped IDbContextFactory — connection string provider'dan
/// tenant-specific DbContext olusturur.
/// Func{string} kullanarak DataAccess -> Business dongusel referansini onler.
/// DI'da bridge: () => tenantContext.ConnectionString
/// </summary>
public sealed class TenantDbContextFactory(
    Func<string> connectionStringProvider,
    ILoggerFactory loggerFactory) : IDbContextFactory<IntegrationDbContext>
{
    public IntegrationDbContext CreateDbContext()
    {
        var connectionString = connectionStringProvider();

        // Enforce connection pool limit per tenant to prevent pool exhaustion
        if (!connectionString.Contains("Maximum Pool Size", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("MaxPoolSize", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = connectionString.TrimEnd(';') + ";Maximum Pool Size=10;";
        }

        var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.UseLoggerFactory(loggerFactory);

        return new IntegrationDbContext(optionsBuilder.Options);
    }
}
