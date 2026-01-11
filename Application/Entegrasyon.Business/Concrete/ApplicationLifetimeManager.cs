using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public class ApplicationLifetimeManager : IApplicationLifetimeManager
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<ApplicationLifetimeManager> _logger;
    public ApplicationLifetimeManager(IntegrationDbContext context, ILogger<ApplicationLifetimeManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ApplyStartActions()
    {
        await MigrateDatabase();
        _logger.LogInformation("Uygulama başlatıldı.");
    }
    private async Task MigrateDatabase(CancellationToken ct = default)
    {
        var db = _context.Database;
        var pendingMigrations = await db.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
        {
            await db.MigrateAsync(ct);
        }
    }
}
