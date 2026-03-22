using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.FileStorage;

namespace Entegrasyon.Business.Concrete;

public class ApplicationLifetimeManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<ApplicationLifetimeManager> logger,
    IMinioFileStorage minioFileStorage,
    IHostEnvironment hostEnvironment)
    : IApplicationLifetimeManager
{
    public async Task ApplyStartActions()
    {
        if (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"))
            await MigrateDatabase();

        await minioFileStorage.EnsureBucketExistsAsync();
        logger.LogInformation("Uygulama başlatıldı.");
    }

    private async Task MigrateDatabase(CancellationToken ct = default)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct);
        var pending = pendingMigrations.ToList();

        if (pending.Count == 0)
            return;

        logger.LogInformation("Pending migration'lar uygulanıyor: {Migrations}", string.Join(", ", pending));
        await dbContext.Database.MigrateAsync(ct);
        logger.LogInformation("{Count} migration başarıyla uygulandı.", pending.Count);
    }
}
