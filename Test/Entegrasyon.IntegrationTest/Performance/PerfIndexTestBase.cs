using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Performance;

/// <summary>
/// Index/EXPLAIN performans testleri icin hafif base.
///
/// WebApplicationFactory KULLANMAZ — sadece migrate edilmis bir PostgreSQL + dogrudan
/// kurulan IntegrationDbContext gerekir. Bu sayede uygulama startup seeder'inin
/// (ApplicationStarted → AdminPermissionSeeder) migrate'ten once kosma yarisindan etkilenmez
/// ve test tamamen izole/deterministik kalir. Her test class'i kendi Testcontainers
/// PostgreSQL container'ini (IClassFixture) kullanir.
/// </summary>
public abstract class PerfIndexTestBase : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _pg;

    protected PerfIndexTestBase(PostgreSqlFixture pg) => _pg = pg;

    protected IntegrationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseNpgsql(_pg.ConnectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        return new IntegrationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
        await SeedAsync(db);

        // Planlayicinin guncel istatistikle calismasi icin tum tabloyu analiz et.
        await AnalyzeAsync(db);
    }

    /// <summary>Alt class kendi test verisini seed eder (migrate sonrasi).</summary>
    protected abstract Task SeedAsync(IntegrationDbContext db);

    /// <summary>Hangi tablonun ANALYZE edilecegi.</summary>
    protected abstract string TableToAnalyze { get; }

    private async Task AnalyzeAsync(IntegrationDbContext db)
    {
        // TableToAnalyze sabit, kod-ici deger (kullanici girdisi degil); concat ile EF1002 tetiklenmez.
        var sql = "ANALYZE \"" + TableToAnalyze + "\";";
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
