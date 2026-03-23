using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;

namespace Entegrasyon.IntegrationTest.Fixtures;

/// <summary>
/// Tum integration testleri icin base class.
/// Her test class'i icin WebApplicationFactory olusturulur, her test sonrasi Respawn ile DB temizlenir.
/// Seed tabloları (Roles, Users, ApplicationSettings vb.) Respawn tarafindan korunur.
/// </summary>
[Trait("Category", "Integration")]
[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlFixture _pgFixture;
    private IntegrationTestWebAppFactory _factory = null!;
    private Respawner _respawner = null!;
    private NpgsqlConnection _dbConnection = null!;

    protected IServiceProvider Services => _factory.Services;
    protected IServiceScope CreateScope() => Services.CreateScope();

    protected IntegrationTestBase(PostgreSqlFixture pgFixture)
    {
        _pgFixture = pgFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new IntegrationTestWebAppFactory(_pgFixture.ConnectionString);

        // Force the WebApplicationFactory to build the host
        _ = _factory.Server;

        // Apply migrations
        using var scope = Services.CreateScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        using var dbContext = contextFactory.CreateDbContext();
        await dbContext.Database.MigrateAsync();

        // Setup Respawn — skip seed tables
        _dbConnection = new NpgsqlConnection(_pgFixture.ConnectionString);
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore =
            [
                new Respawn.Graph.Table("__EFMigrationsHistory"),
                new Respawn.Graph.Table("Roles"),
                new Respawn.Graph.Table("Users"),
                new Respawn.Graph.Table("UsersRoles"),
                new Respawn.Graph.Table("ApplicationSettings")
            ]
        });

        await OnInitializeAsync();
    }

    /// <summary>
    /// Alt class'lar icin ek initialization hook'u.
    /// </summary>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Reset DB state for next test class
        if (_dbConnection is { State: System.Data.ConnectionState.Open })
        {
            await _respawner.ResetAsync(_dbConnection);
            await _dbConnection.DisposeAsync();
        }

        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Scoped bir DbContext olusturur. Her cagrida yeni scope + yeni context.
    /// </summary>
    protected IntegrationDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        return factory.CreateDbContext();
    }

    /// <summary>
    /// Scoped servis resolve eder. Dikkat: scope yonetimi caller'a aittir.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Scoped servis resolve eder ve scope ile birlikte doner.
    /// Caller scope'u dispose etmelidir.
    /// </summary>
    protected (T Service, IServiceScope Scope) GetScopedService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<T>();
        return (service, scope);
    }
}
