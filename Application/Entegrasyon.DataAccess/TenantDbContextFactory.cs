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
