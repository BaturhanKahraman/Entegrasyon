using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public class ApplicationLifetimeManager(IntegrationDbContext context, ILogger<ApplicationLifetimeManager> logger)
    : IApplicationLifetimeManager
{
    public async Task ApplyStartActions()
    {
        await MigrateDatabase();
        logger.LogInformation("Uygulama başlatıldı.");
    }
    private async Task MigrateDatabase(CancellationToken ct = default)
    {
        var db = context.Database;
        var pendingMigrations = await db.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
        {
            await db.MigrateAsync(ct);
        }
    }
}
