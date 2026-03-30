using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Entegrasyon.DataAccess;

/// <summary>
/// EF Core CLI migration komutları için design-time factory.
/// Multi-tenant IDbContextFactory kullanıldığından, CLI DbContext oluşturamıyor —
/// bu sınıf appsettings.Development.json'dan connection string okuyarak çözer.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IntegrationDbContext>
{
    public IntegrationDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Entegrasyon.Blazor");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Main")
            ?? throw new InvalidOperationException("Connection string 'Main' not found in appsettings.");

        var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

        return new IntegrationDbContext(optionsBuilder.Options);
    }
}
