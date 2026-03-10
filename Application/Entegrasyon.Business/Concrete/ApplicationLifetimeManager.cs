using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;

namespace Entegrasyon.Business.Concrete;

public class ApplicationLifetimeManager(
    IntegrationDbContext context,
    ILogger<ApplicationLifetimeManager> logger,
    IMinioFileStorage minioFileStorage)
    : IApplicationLifetimeManager
{
    public async Task ApplyStartActions()
    {
        await MigrateDatabase();
        await minioFileStorage.EnsureBucketExistsAsync();
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
