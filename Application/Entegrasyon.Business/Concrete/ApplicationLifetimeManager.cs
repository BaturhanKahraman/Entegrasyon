using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class ApplicationLifetimeManager
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
        //handle multiple databases.
        //you can use connection string placeholder but need to handle multiple database when needed.
        var db = _context.Database;
        var pendingMigrations = await db.GetPendingMigrationsAsync(ct);
        if (pendingMigrations.Any())
        {
            await db.MigrateAsync(ct);
        }
    }

}
